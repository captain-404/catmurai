const TALENTS=[
{id:'edge',branch:'Blade',name:'Keen Edge',icon:'⚔',max:3,description:'+4% all damage per rank.',effect:'damage',value:.04},
{id:'focus',branch:'Blade',name:'Moon Focus',icon:'☾',max:3,parent:'edge',description:'+6% skill damage per rank.',effect:'skill',value:.06},
{id:'execute',branch:'Blade',name:'Final Judgment',icon:'✧',max:3,parent:'focus',description:'+8% damage per rank against foes below 35% health.',effect:'execute',value:.08},
{id:'vitality',branch:'Guardian',name:'Nine Lives',icon:'♥',max:3,description:'+20 maximum health per rank.',effect:'health',value:20},
{id:'armor',branch:'Guardian',name:'Crimson Guard',icon:'♜',max:3,parent:'vitality',description:'3% less incoming damage per rank.',effect:'armor',value:.03},
{id:'renewal',branch:'Guardian',name:'Second Wind',icon:'❖',max:3,parent:'armor',description:'+0.4 health regenerated each second per rank.',effect:'regen',value:.4},
{id:'spirit',branch:'Spirit',name:'Quiet Mind',icon:'◈',max:3,description:'+0.5 spirit regenerated each second per rank.',effect:'mana',value:.5},
{id:'mending',branch:'Spirit',name:'Sacred Paw',icon:'✦',max:3,parent:'spirit',description:'+8% healing from skills and potions per rank.',effect:'healing',value:.08},
{id:'flow',branch:'Spirit',name:'Perfect Flow',icon:'⌛',max:3,parent:'mending',description:'3% shorter skill cooldowns per rank.',effect:'cooldown',value:.03}];
function talentBonus(effect){return TALENTS.filter(t=>t.effect===effect).reduce((sum,t)=>sum+(p.talents?.[t.id]||0)*t.value,0)}
function talentSpent(){return TALENTS.reduce((n,t)=>n+(p.talents?.[t.id]||0),0)}
function talentPoints(){return Math.max(0,p.level-1-talentSpent())}
function sanitizeTalents(){const raw=p.talents||{},clean={};let remaining=Math.max(0,Math.floor(p.level)-1);for(const t of TALENTS){let rank=Number.isInteger(raw[t.id])?Math.max(0,Math.min(t.max,raw[t.id],remaining)):0;if(t.parent&&clean[t.parent]!==3)rank=0;clean[t.id]=rank;remaining-=rank}p.talents=clean}
function learnTalent(id){const t=TALENTS.find(t=>t.id===id);if(!t||talentPoints()<1||(p.talents[t.id]||0)>=t.max||(t.parent&&p.talents[t.parent]!==3))return false;p.talents[t.id]=(p.talents[t.id]||0)+1;save();return true}
function resetTalents(){p.talents={};sanitizeTalents();p.hp=Math.min(p.hp,maxhp());save()}

