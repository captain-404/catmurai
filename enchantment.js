/* Blade-local enchant VFX. Coordinates are registered to each authored sprite. */
const ENCHANT_TIERS=[
 {min:0,name:'Unenchanted',color:'#bcc9dc'},
 {min:1,name:'Frost edge',color:'#a7e5ff'},
 {min:4,name:'Azure flame',color:'#428dff'},
 {min:7,name:'Violet storm',color:'#995aff'},
 {min:10,name:'Arcane radiance',color:'#eb64ff'},
 {min:15,name:'Crimson ascension',color:'#ff466f'}
];
function enchantStyle(value){const level=Number.isFinite(Number(value))?Math.max(0,Math.floor(Number(value))):0;let tier=0;for(let i=1;i<ENCHANT_TIERS.length;i++)if(level>=ENCHANT_TIERS[i].min)tier=i;return {...ENCHANT_TIERS[tier],tier,level,power:Math.min(level,20)/20}}
const BLADE_POSES={
 walk:[[195,268,385,351],[194,268,379,350],[218,272,403,354],[222,264,408,348],[198,269,388,349],[209,268,401,351],[219,265,407,347],[220,266,408,348]],
 0:[[130,189,256,239],[128,187,251,241],[137,188,254,245],[123,185,238,243],[145,188,264,236],[137,188,246,241]],
 1:[[104,176,196,222],[202,85,253,105],[130,30,254,84],[192,165,252,184],[205,115,312,87],[115,173,215,225]],
 2:[[62,175,9,211],null,null,null,null,[107,178,171,234]],
 3:[[116,173,249,224],[167,170,258,207],null,null,[86,174,5,214],[85,172,187,222]]
};
function drawWeaponEnchant(sheet,frame,anchor,foot,scale,level=p.weapon,clock=time){const style=enchantStyle(level),blade=BLADE_POSES[sheet]?.[frame];if(!style.tier||!blade)return;const [ax,ay,bx,by]=blade.map((v,i)=>(v-(i%2?foot:anchor))*scale),dx=bx-ax,dy=by-ay,len=Math.hypot(dx,dy);ctx.save();ctx.translate(ax,ay);ctx.rotate(Math.atan2(dy,dx));ctx.globalCompositeOperation='lighter';ctx.lineCap='round';const alpha=ctx.globalAlpha,pulse=.88+Math.sin(clock*3)*.12;
 // Soft overlapping halos retain the steel silhouette beneath the light.
 for(let pass=0;pass<3;pass++){ctx.globalAlpha=alpha*(.045+style.power*.045)*pulse;ctx.strokeStyle=style.color;ctx.lineWidth=(8+style.power*25)/(pass+1);ctx.shadowColor=style.color;ctx.shadowBlur=10+style.power*18;ctx.beginPath();ctx.moveTo(2,0);ctx.lineTo(len,0);ctx.stroke()}
 ctx.shadowBlur=7;ctx.globalAlpha=alpha*(.35+style.power*.35);ctx.strokeStyle=style.color;ctx.lineWidth=1.2;ctx.beginPath();ctx.moveTo(0,1);ctx.lineTo(len,1);ctx.stroke();
 if(style.tier>=2){ctx.shadowBlur=5;for(let ribbon=0;ribbon<(style.tier>=4?3:2);ribbon++){ctx.globalAlpha=alpha*(.22+style.power*.16);ctx.strokeStyle=ribbon===1&&style.tier===5?'#b078ff':style.color;ctx.lineWidth=.8+style.power;ctx.beginPath();for(let i=0;i<=24;i++){const u=i/24,v=Math.sin(u*Math.PI)*Math.sin(u*9-clock*3+ribbon*2.5)*(3+style.power*12);i?ctx.lineTo(u*len,v):ctx.moveTo(0,v)}ctx.stroke()}}
 const count=style.tier>=3?Math.min(12,style.tier*2):0;ctx.shadowBlur=6;ctx.fillStyle='#e9dcff';for(let i=0;i<count;i++){const u=(i/count+clock*(.12+style.power*.1))%1;ctx.globalAlpha=alpha*Math.sin(u*Math.PI)*(.3+style.power*.4);const y=Math.sin(i*8+clock*2)*Math.sin(u*Math.PI)*(4+style.power*13);ctx.beginPath();ctx.arc(u*len,y,.6+style.power*.8,0,Math.PI*2);ctx.fill()}ctx.restore()}

