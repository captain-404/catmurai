/* Weapon-class pose atlas and combat cadence. */
const CLASS_PROFILES={sword:{duration:.72,impact:.36,interval:.85},axe:{duration:1.12,impact:.56,interval:1.3,row:0},dual:{duration:.76,impact:.28,second:.47,interval:.88,row:1},greatsword:{duration:1.3,impact:.65,interval:1.5,row:2}};
function weaponClass(){return equippedWeapon().type||'sword'}
function syncWeaponClips(){const c=CLASS_PROFILES[weaponClass()];if(weaponClass()==='sword'){CLIPS.attack.duration=.72;CLIPS.attack.impact=.36;CLIPS.moon.duration=.86;CLIPS.moon.impact=.5;CLIPS.whirl.duration=.95;CLIPS.whirl.impact=.32;return}CLIPS.attack.duration=c.duration;CLIPS.attack.impact=c.impact;CLIPS.moon.duration=c.duration+.16;CLIPS.moon.impact=c.impact+.08;CLIPS.whirl.duration=c.duration+.25;CLIPS.whirl.impact=c.impact}
const classEquip=equipWeapon;equipWeapon=function(id){const ok=classEquip(id);if(ok)syncWeaponClips();return ok};syncWeaponClips();
const classStart=startMotion;startMotion=function(name){syncWeaponClips();classStart(name)};
const classHit=hit;hit=function(e,damage){const c=CLASS_PROFILES[weaponClass()];if(weaponClass()==='sword')return classHit(e,damage);if(e.dead)return;if(!busy())startMotion('attack');const start=motion.events.length;classHit(e,damage);if(c.second&&motion.events.length>start){const first=motion.events[start];first.hitScale=.5;motion.events.push({...first,remaining:first.remaining+(c.second-c.impact),damage:first.damage,hitScale:.5})}};
// Rectangles lie in the empty gutters of v2, never through character silhouettes.
const CLASS_CROPS=[
 [[20,15,220,244,129,247],[277,25,215,231,384,248],[530,25,218,231,631,248],[790,10,205,246,902,248]],
 [[14,292,225,205,120,484],[270,303,238,194,375,484],[533,293,217,204,631,484],[782,294,237,204,888,484]],
 [[19,517,220,225,126,729],[276,520,216,222,380,728],[528,520,226,222,633,729],[784,508,231,234,899,731]],
 [[12,779,244,194,119,960],[258,779,272,194,383,960],[519,777,247,196,633,961],[754,778,265,195,893,962]],
 [[17,1001,223,237,126,1214],[273,1004,238,234,382,1218],[528,1005,234,233,637,1219],[791,982,211,256,908,1223]],
 [[10,1268,265,213,119,1463],[258,1269,267,212,384,1467],[519,1269,243,212,637,1467],[781,1268,237,213,894,1467]]
];
const classImage=new Image(),classFrames=[];
classImage.onload=()=>{try{const s=document.createElement('canvas');s.width=classImage.naturalWidth;s.height=classImage.naturalHeight;if(s.width!==1024||s.height!==1536)throw Error('Unexpected weapon atlas dimensions');const c=s.getContext('2d',{willReadFrequently:true});c.drawImage(classImage,0,0);const im=c.getImageData(0,0,s.width,s.height);for(let i=0;i<im.data.length;i+=4){const d=im.data,g=d[i+1]-Math.max(d[i],d[i+2]);if(g>25&&d[i+1]>85){d[i+3]=Math.round(255*(1-Math.min(1,(g-25)/65)));d[i+1]=Math.min(d[i+1],Math.max(d[i],d[i+2])+15)}}c.putImageData(im,0,0);for(let row=0;row<3;row++){classFrames[row]=[];for(let col=0;col<8;col++){const [sx,sy,w,h,cx,fy]=CLASS_CROPS[row*2+Math.floor(col/4)][col%4];const tile=isolateFrame(s,sx,sy,w,h,true);const pixels=tile.getContext('2d').getImageData(0,0,w,h).data;let clipped=0,edgePoints=[];for(let y=0;y<h;y++)for(let x=0;x<w;x++)if((x===0||x===w-1||y===0||y===h-1)&&pixels[(y*w+x)*4+3]>40){clipped++;edgePoints.push([x,y])}if(clipped)throw Error('Frame '+row+':'+col+' touches crop edge '+JSON.stringify(edgePoints));classFrames[row].push({tile,anchor:cx-sx,foot:fy-sy,sx,sy})}}document.body.dataset.weaponClasses='ready'}catch(e){document.body.dataset.weaponClasses='error';document.title=e.message;log('Weapon animation load failed: '+e.message)}};classImage.src='assets/weapon-classes-v3.png';
function classSchedule(state,type=weaponClass()){
 const c=CLASS_PROFILES[type];if(state==='walk')return {duration:.8,loop:true,keys:[[0,1],[.4,2]]};
 if(['mend','guard','potion'].includes(state)){const duration=CLIPS[state].duration;return {duration,keys:[[0,0],[duration*.18,7],[duration*.82,6]]}}
 if(['idle','hurt','death'].includes(state))return {duration:CLIPS[state].duration,loop:state==='idle',keys:[[0,0]]};
 const duration=c.duration+(state==='moon'?.16:state==='whirl'?.25:0),impact=c.impact+(state==='moon'?.08:0);
 return {duration,keys:[[0,0],[impact*.45,3],[impact,4],[c.second?c.second+(state==='moon'?.08:0):impact+duration*.13,5],[duration*.86,6]]}
}
function classFrame(state,elapsed,type=weaponClass()){const s=classSchedule(state,type),t=s.loop?Math.max(0,elapsed)%s.duration:Math.min(s.duration,Math.max(0,elapsed));let frame=s.keys[0][1];for(const [at,col] of s.keys)if(t>=at-1e-8)frame=col;return frame}
const CLASS_EDGES=[
 [[[197,157,216,203]],[[446,150,466,199]],[[700,154,720,201]],[[851,31,887,52]],[[209,425,217,477]],[[489,423,493,473]],[[705,381,725,428]],[[981,336,1006,380]]],
 [[[83,667,35,700],[180,667,217,695]],[[368,669,404,688],[432,668,478,689]],[[610,670,642,691],[687,666,733,685]],[[847,555,891,528],[955,666,998,687]],[[173,879,247,854]],[[446,870,516,843],[320,888,274,904]],[[586,905,535,931],[694,901,750,929]],[[817,876,781,850],[963,875,998,846]]],
 [[[141,1159,223,1214]],[[401,1164,481,1213]],[[661,1164,748,1213]],[[876,1034,827,1001]],[[135,1388,266,1401]],[[418,1395,514,1447]],[[675,1405,749,1461]],[[854,1393,1000,1393]]]
];
const swordPose=drawPose;drawPose=function(state,elapsed,x,y,face=1,opacity=1){const type=weaponClass(),r=CLASS_PROFILES[type].row;if(r===undefined||classFrames[r]?.length!==8)return swordPose(state,elapsed,x,y,face,opacity);const col=classFrame(state,elapsed,type),f=classFrames[r][col],scale=1.05;ctx.save();ctx.globalAlpha*=opacity;ctx.translate(x,y);ctx.scale(face,1);if(state==='idle')ctx.scale(1,1+Math.sin(elapsed*3)*.005);if(state==='walk')ctx.translate(0,-Math.abs(Math.sin(elapsed*10))*2);if(state==='hurt')ctx.translate(-Math.sin(elapsed*10)*8,0);if(state==='death')ctx.rotate(-Math.min(1,elapsed/1.25)*1.45);ctx.drawImage(f.tile,-f.anchor*scale,-f.foot*scale,f.tile.width*scale,f.tile.height*scale);for(const edge of CLASS_EDGES[r][col]){BLADE_POSES.classWeapon=[edge.map((v,i)=>v-(i%2?f.sy:f.sx))];armoryGlow('classWeapon',0,f.anchor,f.foot,scale)}ctx.restore()};




// Step the visible weapon poses instead of the unrelated katana atlas indices.
const originalAnimationStep=$('#animationStep').onclick;
$('#animationStep').onclick=()=>{if(weaponClass()==='sword')return originalAnimationStep();motion.paused=true;$('#animationPause').textContent='Resume';const s=classSchedule(motion.state);const next=s.keys.find(([at])=>at>motion.elapsed+.0001);motion.elapsed=next?next[0]:0;motion.fx.forEach(f=>f.elapsed=motion.elapsed)};


