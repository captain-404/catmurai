/* Locomotion-only presentation. Combat pose selection and timing stay untouched. */
const locomotion={player:{blend:1,previous:null,state:'idle',elapsed:0,lean:0},actors:new WeakMap(),current:null};
function locomotionEase(value,target,dt,rate=14){return target+(value-target)*Math.exp(-rate*Math.max(0,dt))}
function advanceLocomotion(s,state,elapsed,dx,dt){
 if(s.state!==state){s.previous=['idle','walk'].includes(s.state)&&['idle','walk'].includes(state)?{state:s.state,elapsed:s.elapsed}:null;s.blend=s.previous?0:1;s.state=state}
 s.elapsed=elapsed;s.blend=Math.min(1,s.blend+dt/.14);s.lean=locomotionEase(s.lean,state==='walk'?Math.max(-1,Math.min(1,dx/Math.max(.001,dt)/160))*.014:0,dt);
 if(s.blend>=1)s.previous=null;
}
const locomotionUpdate=update;update=function(dt){const enabled=running&&!$('#modal').open&&!document.hidden&&!motion.preview,x=p.x;const before=enemies.map(e=>({e,x:e.x}));locomotionUpdate(dt);if(!enabled)return;
 advanceLocomotion(locomotion.player,motion.state,motion.elapsed,p.x-x,dt);
 for(const b of before){const n=npcFrame(b.e);let s=locomotion.actors.get(b.e);if(!s){s={blend:1,previous:null,state:'idle',elapsed:0,lean:0};locomotion.actors.set(b.e,s)}advanceLocomotion(s,n.attack>0?'attack':n.moving?'walk':'idle',n.phase,b.e.x-b.x,dt)}
};
const locomotionPose=drawPose;drawPose=function(state,t,x,y,face=1,opacity=1){if(!['idle','walk'].includes(state))return locomotionPose(state,t,x,y,face,opacity);const s=locomotion.player;ctx.save();ctx.translate(x,y);if(state==='walk'&&!motion.preview)ctx.transform(1,0,-s.lean,1,0,0);
 const transition=!motion.preview&&s.state===state&&s.previous&&(typeof shadow==='undefined'||!shadow.active||shadow.elapsed>.4);
 if(transition){const u=s.blend*s.blend*(3-2*s.blend);locomotionPose(s.previous.state,s.previous.elapsed,0,0,face,opacity*(1-u));locomotionPose(state,t,0,0,face,opacity*u)}else locomotionPose(state,t,0,0,face,opacity);ctx.restore()
};
const locomotionActor=actor;actor=function(a,kind){const old=locomotion.current;locomotion.current=kind?{a,s:locomotion.actors.get(a)}:null;try{return locomotionActor(a,kind)}finally{locomotion.current=old}};
const locomotionNpc=npcSprite;npcSprite=function(row,col,x,y,height,face=1){const current=locomotion.current,s=current?.s;if(col>=3||s?.state==='attack')return locomotionNpc(row,col,x,y,height,face);ctx.save();ctx.translate(x,y);
 if(s?.state==='walk')ctx.transform(1,0,-s.lean,1,0,0);else if(col===0){const phase=time*(row===1?2.6:row===3?1.45:1.9)+(current?.a.id||0)*1.73;ctx.scale(1-Math.sin(phase)*.0015,1+Math.sin(phase)*.004)}
 let result;if(s?.previous&&s.blend<1){const u=s.blend*s.blend*(3-2*s.blend),previousCol=s.previous.state==='idle'?0:[1,2][Math.floor(s.previous.elapsed*2)%2];ctx.save();ctx.globalAlpha*=1-u;locomotionNpc(row,previousCol,0,0,height,face);ctx.restore();ctx.globalAlpha*=u;result=locomotionNpc(row,col,0,0,height,face)}else result=locomotionNpc(row,col,0,0,height,face);ctx.restore();return result
};
