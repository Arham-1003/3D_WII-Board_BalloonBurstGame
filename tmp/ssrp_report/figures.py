"""Scientific figures from independently calculated arrays, without editing inputs."""
import json, html, math
from pathlib import Path
import numpy as np
from analyse import ROOT, FILTERS, filt, response, group_ms

OUT=ROOT/'figures'; OUT.mkdir(exist_ok=True)
COL={'Raw':'#727b83','Median':'#008577','MovingAverage':'#2459a6','Butterworth':'#b34c16','AlphaBeta':'#7848a5'}
LABEL={'Median':'Median N = 3','MovingAverage':'Moving average N = 5','Butterworth':'Butterworth as recorded','AlphaBeta':'Alpha beta'}

class Figure:
    def __init__(self,w,h):
        self.w=w; self.h=h
        self.parts=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}"><rect width="100%" height="100%" fill="white"/>']
    def text(self,x,y,s,size=22,anchor='start',color='#20252a',rotate=None):
        tr=f' transform="rotate({rotate},{x},{y})"' if rotate else ''
        self.parts.append(f'<text x="{x}" y="{y}" font-family="Arial" font-size="{size}" text-anchor="{anchor}" fill="{color}"{tr}>{html.escape(str(s))}</text>')
    def line(self,x1,y1,x2,y2,color='#d6dce2',width=1,dash=''):
        self.parts.append(f'<path d="M {x1} {y1} L {x2} {y2}" stroke="{color}" stroke-width="{width}" fill="none" stroke-dasharray="{dash}"/>')
    def chart(self,box,xlim,ylim,xticks,yticks,title,xlabel,ylabel,series,logy=False):
        x,y,w,h=box
        def tx(a): return x+(np.asarray(a)-xlim[0])/(xlim[1]-xlim[0])*w
        def ty(a):
            a=np.asarray(a)
            if logy: return y+h-(np.log10(np.maximum(a,10**ylim[0]))-ylim[0])/(ylim[1]-ylim[0])*h
            return y+h-(a-ylim[0])/(ylim[1]-ylim[0])*h
        for v in xticks:
            z=float(tx(v)); self.line(z,y,z,y+h); self.text(z,y+h+28,f'{v:g}',20,'middle')
        for v in yticks:
            z=float(ty(10**v if logy else v)); self.line(x,z,x+w,z)
            self.text(x-12,z+7,f'1e{v:g}' if logy else f'{v:g}',20,'end')
        self.line(x,y+h,x+w,y+h,'#40474d',2); self.line(x,y,x,y+h,'#40474d',2)
        self.text(x,y-21,title,25); self.text(x+w/2,y+h+62,xlabel,22,'middle')
        self.text(x-65,y+h/2,ylabel,21,'middle',rotate=-90)
        for key,xx,yy in series:
            xx=np.asarray(xx); yy=np.asarray(yy); mask=(xx>=xlim[0])&(xx<=xlim[1])&np.isfinite(yy)
            px=tx(xx[mask]); py=np.clip(ty(yy[mask]),y,y+h)
            coords=' '.join(('M' if i==0 else 'L')+f'{a:.2f},{b:.2f}' for i,(a,b) in enumerate(zip(px,py)))
            self.parts.append(f'<path d="{coords}" stroke="{COL.get(key,key)}" stroke-width="{2.3 if key=="Raw" else 3}" fill="none"/>')
    def legend(self,y,keys):
        for i,key in enumerate(keys):
            if len(keys)>3:
                x=[45,400,780][i%3]; yy=y-28+(i//3)*30
            else:
                x=45+i*390; yy=y
            self.line(x,yy,x+35,yy,COL[key],4); self.text(x+44,yy+7,LABEL.get(key,key),18)
    def save(self,name):
        (OUT/(name+'.svg')).write_text(''.join(self.parts)+'</svg>',encoding='utf-8')

def main():
    d=json.loads((ROOT/'calculations.json').read_text()); z=np.load(ROOT/'series.npz')
    # Frequency response, linear filters only. MA phase discontinuities at zeros are excluded.
    f=Figure(1200,850); freq=np.linspace(.01,6,1400)
    names=['MovingAverage','Butterworth','AlphaBeta']
    f.chart((110,65,1025,270),(0,6),(0,1.1),[0,1,2,3,4,5,6],[0,.25,.5,.707,1],
            'A  Amplitude response','Frequency Hz','Amplitude gain',[(k,freq,np.abs(response(k,freq))) for k in names])
    freq2=np.linspace(.05,3,650)
    f.chart((110,470,1025,255),(0,3),(-20,300),[0,.5,1,1.5,2,2.5,3],[0,100,200,300],
            'B  Group delay below the first moving average zero','Frequency Hz','Group delay ms',[(k,freq2,np.array([group_ms(k,float(v)) for v in freq2])) for k in names])
    f.legend(825,names); f.save('frequency')
    # Fully causal step/ramp/spike experiments, clearly not participant recordings.
    t=np.arange(0,4,1/20); step=(t>=1).astype(float); ramp=np.clip((t-1),0,1); spike=np.zeros_like(t); spike[20]=1
    f=Figure(1200,1110)
    for i,(title,raw) in enumerate([('A  Unit step',step),('B  Ramp lasting one second',ramp),('C  Isolated single sample spike',spike)]):
        f.chart((110,50+i*342,1025,220),(0,3),(-.1,1.15),[0,.5,1,1.5,2,2.5,3],[0,.5,1],title,'Time seconds','Output',
            [('Raw',t,raw)]+[(k,t,filt(raw,k)) for k in FILTERS])
    f.legend(1090,['Raw']+FILTERS); f.save('transients')
    # One identical recorded ML input processed through all four filters.
    raw=z['Arham_104729_raw']; tt=z['Arham_104729_t']; good=z['Arham_104729_good']; rv=raw[good]; tv=tt[good]
    candidates=np.arange(5,76)
    start=int(max(candidates,key=lambda a:np.std(rv[(tv>=a)&(tv<=a+8),0])))
    outputs={k:filt(rv,k) for k in FILTERS}
    selected=(tv>=start)&(tv<=start+8)
    lower=float(min([rv[selected,0].min()]+[a[selected,0].min() for a in outputs.values()]))
    upper=float(max([rv[selected,0].max()]+[a[selected,0].max() for a in outputs.values()]))
    bounds=(math.floor((lower-.03)*10)/10,math.ceil((upper+.03)*10)/10)
    f=Figure(1200,1080)
    for i,k in enumerate(FILTERS):
        out=outputs[k]
        f.chart((110,45+i*255,1025,155),(start,start+8),bounds,list(range(start,start+9,2)),np.linspace(bounds[0],bounds[1],3),LABEL[k], 'Round time seconds','CoP ratio',
                [('Raw',tv,rv[:,0]),(k,tv,out[:,0])])
    f.save('common_trace')
    (ROOT/'trace_selection.json').write_text(json.dumps({'start':start,'end':start+8,'method':'largest ML standard deviation among eight second windows beginning at integer seconds 5 through 75'}),encoding='utf-8')
    # PSD by axis, each trial normalized to equal unit integrated power, then averaged.
    freq=z['Arham_104729_frequency']; specs={}
    for axis,key in [('ML','psdx'),('AP','psdy')]:
        values=[]
        for member in ['Akshat','Alka','Arham','Dharti']:
            for seed in [104729,130363,155921,196613]:
                p=z[f'{member}_{seed}_{key}']; values.append(p/np.trapezoid(p,freq))
        specs[axis]=np.mean(values,axis=0)
    f=Figure(1200,510)
    f.chart((115,55,1020,340),(0,10),(-6,1),[0,1,2,4,6,8,10],[-6,-4,-2,0],
            'Normalized raw CoP spectra averaged across 16 rounds','Frequency Hz','Normalized PSD per Hz',
            [('MovingAverage',freq,specs['ML']),('Butterworth',freq,specs['AP'])],True)
    f.line(340,490,380,490,COL['MovingAverage'],4); f.text(390,497,'ML horizontal',21)
    f.line(650,490,690,490,COL['Butterworth'],4); f.text(700,497,'AP vertical',21)
    f.save('spectrum')
    # Compact pipeline diagram with audit branch.
    f=Figure(1200,365)
    boxes=[(20,50,205,85,['Wii Balance Board','Bluetooth']), (280,50,255,85,['Walker and Recorder','Calibrated text rows']),
           (590,50,230,85,['Python bridge','Audit and UDP']), (875,50,300,85,['Unity processing','CoP and selected filter']),
           (875,230,300,85,['Calibration and mapping','Pointer and balloon game']), (470,230,330,85,['Preserved data','Recorder and audit and trial logs'])]
    for x,y,w,h,lines in boxes:
        f.parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="6" fill="#f1f4f7" stroke="#7e8b96" stroke-width="2"/>')
        for i,s in enumerate(lines):f.text(x+w/2,y+33+29*i,s,22,'middle')
    for x1,y1,x2,y2 in [(225,92,280,92),(535,92,590,92),(820,92,875,92),(1025,135,1025,230),(705,135,635,230),(875,273,800,273)]:
        f.line(x1,y1,x2,y2,'#46535f',3)
        angle=math.atan2(y2-y1,x2-x1)
        for a in [-.5,.5]:f.line(x2,y2,x2-12*math.cos(angle+a),y2-12*math.sin(angle+a),'#46535f',3)
    f.text(850,28,'UDP 127.0.0.1 port 5005',19,'middle');f.save('pipeline')
    # Validate generic test expectations and save quantitative simulated response summary.
    tests={}
    for kind in FILTERS:
        y=filt(step,kind); after=np.flatnonzero(t>=1); crossing=np.flatnonzero(y[after]>=.5)
        tests[kind]={'step_half_time_ms':float(crossing[0]/20*1000) if len(crossing) else None,
            'step_peak':float(np.max(y[after])), 'spike_peak':float(np.max(np.abs(filt(spike,kind))))}
    assert tests['Median']['step_half_time_ms']==50
    assert tests['MovingAverage']['step_half_time_ms']==100
    (ROOT/'synthetic_tests.json').write_text(json.dumps(tests,indent=2),encoding='utf-8')
    print(json.dumps(tests,indent=2))

if __name__=='__main__':main()
