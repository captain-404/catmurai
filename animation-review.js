/* Read-only visual review: registered animation poses, without save mutations. */
running=true;motion.preview='idle';motion.paused=true;
const reviewRows=['walk','attack','mend','whirl'];
draw=function(){ctx.fillStyle='#132333';ctx.fillRect(0,0,W,H);text('CATMURAI · ANIMATION FRAME REVIEW',W/2,25,'#ead4ab',16);const cw=W/6,ch=(H-45)/4;for(let row=0;row<4;row++)for(let col=0;col<(row===0?8:6);col++){const cw=W/(row===0?8:6),name=reviewRows[row],c=CLIPS[name],x=(col+.5)*cw,y=45+(row+1)*ch-25;ctx.strokeStyle='#a1acbc20';ctx.strokeRect(col*cw,45+row*ch,cw,ch);line(col*cw+10,y,(col+1)*cw-10,y,'#d4b77945');ctx.save();ctx.translate(x,y);let scale=Math.min(cw/220,(ch-35)/210);ctx.scale(scale,scale);let idx=c.frames.indexOf(col);if(idx<0)idx=c.frames.length-1;drawPose(name,(idx+.01)*c.duration/c.frames.length,0,0,1);ctx.restore();text(name.toUpperCase()+' '+(col+1),x,y+17,'#c1ccd8',10)}};

