from pathlib import Path
import numpy as np,wave,shutil
p=Path(r'D:\YoutubeChannel\catmurai-unreal');dst=p/'SourceAssets/Release';dst.mkdir(parents=True,exist_ok=True)
shutil.copy2(Path(r'C:\Users\Paran\.codex\generated_images\01a101ad-592e-7de0-adf3-1dee077c7221\exec-abcba821-db23-48fb-bc89-f7eecca72297.png'),dst/'title.png')
shutil.copy2(Path(r'C:\Users\Paran\.codex\generated_images\01a101ad-592e-7de0-adf3-1dee077c7221\exec-f077c00a-9fd0-471f-83fc-c00762c552d1.png'),dst/'paving.png')
rate=44100;rng=np.random.default_rng(404)
def save(name,y):
 y=np.clip(y,-.92,.92);pcm=(y*32767).astype('<i2')
 with wave.open(str(dst/(name+'.wav')),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(pcm.tobytes())
for name,notes,length in [('Start',[220,330,440],1),('Upgrade',[440,660,880],.65),('Menu',[550],.12),('Pickup',[880,1100],.13),('Victory',[220,330,440,660,880],1.8),('Defeat',[220,196,146],1.1),('Ultimate',[110,220,440,880],1.6)]:
 y=np.zeros(int(rate*length))
 for i,freq in enumerate(notes):
  pos=int(i*length/(len(notes)+1)*rate);t=np.arange(len(y)-pos)/rate;y[pos:]+=(np.sin(2*np.pi*freq*t)+.22*np.sin(2*np.pi*freq*2.01*t))*np.exp(-t*(5 if name!='Ultimate' else 2))*np.minimum(1,t*180)*.12
 save('S_'+name,y)
for name,length in [('Slash',.2),('Hurt',.22),('Dash',.3),('Impact',.12)]:
 t=np.arange(int(rate*length))/rate;noise=rng.normal(0,1,len(t));noise=np.convolve(noise,np.ones(7)/7,'same')
 y=noise*np.sin(np.pi*t/length)*np.exp(-t*12)*.4
 if name=='Hurt':y+=np.sin(2*np.pi*(160*t-200*t*t))*np.exp(-t*18)*.18
 save('S_'+name,y)
length=32;t=np.arange(rate*length)/rate;y=np.zeros_like(t)
# Gentle evolving pentatonic ambient bed and sparse bell phrase, seamless fades.
chords=[[110,164.81,220,329.63],[98,146.83,196,293.66],[130.81,196,261.63,392],[110,164.81,220,329.63]]
for k,chord in enumerate(chords):
 a=k*8;env=np.clip((t-a)/1.6,0,1)*np.clip((a+9.6-t)/1.6,0,1)
 for freq in chord:y+=(np.sin(2*np.pi*freq*t)+.18*np.sin(2*np.pi*freq*2*t))*.021*env*(.88+.12*np.sin(t*.7+k))
notes=[440,659.25,587.33,493.88,440,329.63,392,440]
for k in range(16):
 z=t-(k*2+.4);mask=z>=0;freq=notes[k%8];y[mask]+=.07*np.sin(2*np.pi*freq*z[mask]+.25*np.sin(2*np.pi*freq*2*z[mask]))*np.exp(-z[mask]*2.6)*np.minimum(1,z[mask]*70)
y*=np.clip(t/.7,0,1)*np.clip((32-t)/.7,0,1);save('Music_Loop',y)
for n in ['ReleaseAudio.h','ReleaseAudio.cpp']:shutil.copy2(Path(__file__).parent/n,p/'Source/CatmuraiSurvival'/n)
print('RELEASE_SOURCE_ASSETS_PASS')
