/* Local game assistance. Never runs while a dialog, preview, or hidden tab pauses play. */
const farm={enabled:false,patrolIndex:-1,timer:0,status:'Off'};
function farmStatus(message){if(farm.status===message)return;farm.status=message;$('#farmStatus').textContent='Auto-farm · '+message}
function stopAutoFarm(reason='Off'){farm.enabled=false;target=null;destination=null;$('#autoFarm').textContent='Auto-farm [F]: off';$('#autoFarm').setAttribute('aria-pressed','false');farmStatus(reason)}
function toggleAutoFarm(){if(!running||$('#modal').open||motion.preview)return;if(farm.enabled){stopAutoFarm();return}farm.enabled=true;farm.patrolIndex=-1;farm.timer=0;target=null;destination=null;$('#autoFarm').textContent='Auto-farm [F]: ON';$('#autoFarm').setAttribute('aria-pressed','true');farmStatus('Finding enemies');log('Auto-farm enabled · seeking creatures across the valley · WASD or click to take control.')}
function farmCandidate(e){return !e.dead&&e.kind!==2}
function seekFarmOpponents(){const spots=enemies.filter(e=>e.kind!==2).map(e=>({x:e.homeX??e.x,y:e.homeY??e.y}));if(!spots.length){destination=null;farmStatus('No hunting spots available');return}if(farm.patrolIndex<0){farm.patrolIndex=spots.reduce((best,s,i)=>Math.hypot(s.x-p.x,s.y-p.y)<Math.hypot(spots[best].x-p.x,spots[best].y-p.y)?i:best,0)}let spot=spots[farm.patrolIndex%spots.length];if(Math.hypot(spot.x-p.x,spot.y-p.y)<45){farm.patrolIndex=(farm.patrolIndex+1)%spots.length;spot=spots[farm.patrolIndex]}destination={x:spot.x,y:spot.y};farmStatus('Searching the valley · hunting spot '+(farm.patrolIndex+1))}
function farmThink(){if(!farm.enabled||!running||$('#modal').open||document.hidden||motion.preview)return;if(busy())return;
const health=p.hp/maxhp();if(health<.55&&cooldown[2]<=0&&p.mp>=28){skill(2);farmStatus('Healing');return}if(health<.35&&p.potions>0){potion();farmStatus('Using a potion');return}if(health<.2){stopAutoFarm('Stopped · returning to shrine');destination={x:0,y:160};log('Auto-farm stopped: low health. Returning to the shrine.');return}
if(!target||!farmCandidate(target)){target=enemies.filter(farmCandidate).sort((a,b)=>Math.hypot(a.x-p.x,a.y-p.y)-Math.hypot(b.x-p.x,b.y-p.y))[0]||null;destination=null}
if(!target){seekFarmOpponents();return}const distance=Math.hypot(target.x-p.x,target.y-p.y);farmStatus(distance>650?'Seeking '+target.name:distance>65?'Approaching '+target.name:'Fighting '+target.name);if(distance>150)return;
const nearby=enemies.filter(e=>!e.dead&&e.kind!==2&&Math.hypot(e.x-p.x,e.y-p.y)<190).length;
if(nearby>=2&&buff<=0&&cooldown[3]<=0&&p.mp>=46){skill(3);return}if(nearby>=2&&!enemies.some(e=>e.kind===2&&!e.dead&&Math.hypot(e.x-p.x,e.y-p.y)<240)&&cooldown[1]<=0&&p.mp>=42){skill(1);return}if(cooldown[0]<=0&&p.mp>=30)skill(0)
}
const farmingSimulation=update;
update=function(dt){if(farm.enabled&&running&&!$('#modal').open&&!document.hidden&&!motion.preview){farm.timer-=dt;if(farm.timer<=0){farm.timer=.15;farmThink()}}farmingSimulation(dt)};
$('#autoFarm').onclick=toggleAutoFarm;
addEventListener('keydown',e=>{const k=e.key.toLowerCase();if(k==='f'&&!e.repeat&&!['INPUT','TEXTAREA','SELECT'].includes(e.target?.tagName)){e.preventDefault();toggleAutoFarm()}else if(farm.enabled&&['w','a','s','d','tab','escape'].includes(k))stopAutoFarm('Manual control')});
canvas.addEventListener('pointerdown',()=>{if(farm.enabled)stopAutoFarm('Manual control')},true);


