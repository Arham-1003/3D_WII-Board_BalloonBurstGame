"""Read-only source audit and independently reproducible SSRP report calculations.

Runtime: bundled Codex Python 3.12 with NumPy. Original recordings are never edited.
"""
from pathlib import Path
import csv, json, hashlib, math, sys
import numpy as np

ROOT = Path(__file__).resolve().parent
DATA = Path(r'C:\ARHAM\College Downloads\Sem 3\SSRP\SSRP Project details and analysis\Data Acquired\Data Acquired')
FILTERS = ['Median', 'MovingAverage', 'Butterworth', 'AlphaBeta']
MEMBERS = [('Akshat','Median'),('Alka','MovingAverage'),('Arham','Butterworth'),('Dharti','AlphaBeta')]
COLUMNS = {'Median':'median','MovingAverage':'moving_average','Butterworth':'butterworth','AlphaBeta':'alpha_beta'}
FS = 20.

def rows(path):
    with path.open(encoding='utf-8-sig',newline='') as f:
        return list(csv.DictReader(f))

def butter(cutoff=2.5, rate=50.):
    k=np.tan(np.pi*cutoff/rate); d=1/(1+np.sqrt(2)*k+k*k)
    return np.array([k*k*d,2*k*k*d,k*k*d]), np.array([1,2*(k*k-1)*d,(1-np.sqrt(2)*k+k*k)*d])

def coefficients(name):
    if name=='MovingAverage': return np.ones(5)/5, np.array([1.])
    if name=='Butterworth': return butter()
    if name=='AlphaBeta': return np.array([.85,-.80]), np.array([1,-1.10,.15])
    raise ValueError(name)

def filt(x,name,initial=None):
    x=np.asarray(x,dtype=float); y=np.zeros_like(x)
    if name in ('Median','MovingAverage'):
        n=3 if name=='Median' else 5
        for i in range(len(x)):
            y[i]=np.median(x[max(0,i-n+1):i+1],axis=0) if name=='Median' else np.mean(x[max(0,i-n+1):i+1],axis=0)
    elif name=='Butterworth':
        b,a=butter(); x1=x2=y1=y2=np.zeros(x.shape[1:])
        for i,v in enumerate(x):
            out=b[0]*v+b[1]*x1+b[2]*x2-a[1]*y1-a[2]*y2
            y[i]=out; x2=x1; x1=v; y2=y1; y1=out
    elif name=='AlphaBeta':
        p=x[0].copy(); v=np.zeros_like(p); y[0]=p
        for i in range(1,len(x)):
            pred=p+v*.05; residual=x[i]-pred
            p=pred+.85*residual; v=v+(.05/.05)*residual; y[i]=p
    return y

def response(name, f):
    b,a=coefficients(name); q=np.exp(-2j*np.pi*np.asarray(f)/FS)
    return sum(c*q**i for i,c in enumerate(b))/sum(c*q**i for i,c in enumerate(a))

def group_ms(name,f=.5):
    eps=1.e-5
    # derivative of phase in radians with respect to frequency in Hz
    slope=np.angle(response(name,f+eps)/response(name,f-eps))/(2*eps)
    return float(-slope/(2*np.pi)*1000)

def metrics(raw,out):
    ratios=[]; lag_integer=[]; lag_fractional=[]; peaks=[]
    for axis in range(2):
        x=raw[:,axis]; y=out[:,axis]
        ratios.append(float(np.std(np.diff(y),ddof=1)/np.std(np.diff(x),ddof=1)))
        x=x-x.mean(); y=y-y.mean(); n=len(x)
        # Positive lag means output y follows input x. Global energy normalization.
        c=np.correlate(y,x,'full')/np.sqrt(np.dot(x,x)*np.dot(y,y))
        lags=np.arange(-n+1,n); select=np.abs(lags)<=10
        cs=c[select]; ls=lags[select]; p=int(np.argmax(cs)); lag=float(ls[p])
        lag_integer.append(lag/FS*1000)
        correction=0.
        if 0<p<len(cs)-1:
            den=cs[p-1]-2*cs[p]+cs[p+1]
            if abs(den)>1.e-15: correction=float(np.clip(.5*(cs[p-1]-cs[p+1])/den,-.5,.5))
        lag_fractional.append((lag+correction)/FS*1000); peaks.append(float(cs[p]))
    return dict(jitter=float(np.mean(ratios)),jitter_axes=ratios,lag_ms=float(np.mean(lag_integer)),
                lag_axes=lag_integer,interpolated_lag_ms=float(np.mean(lag_fractional)),correlation=peaks)

def psd(x,nperseg=256):
    # Welch method: periodic Hann, 50% overlap, segment mean removal, one-sided density.
    win=.5-.5*np.cos(2*np.pi*np.arange(nperseg)/nperseg)
    spectra=[]
    for start in range(0,len(x)-nperseg+1,nperseg//2):
        s=np.asarray(x[start:start+nperseg]); s=s-s.mean()
        p=np.abs(np.fft.rfft(s*win))**2/(FS*np.dot(win,win))
        p[1:-1]*=2; spectra.append(p)
    return np.fft.rfftfreq(nperseg,1/FS),np.mean(spectra,axis=0)

def spectral_share(freq,power,edge):
    # Integrate PSD with trapezoids, interpolating the boundary exactly.
    mask=freq<edge; f=np.r_[freq[mask],edge]; p=np.r_[power[mask],np.interp(edge,freq,power)]
    return float(np.trapezoid(p,f)/np.trapezoid(power,freq))

def inspect():
    results=[]; arrays={}; manifest=[]
    for member,kind in MEMBERS:
        folder=next((DATA/'Filtered data').glob(member+'*'))
        eventfiles=list(folder.glob('*.events.csv'))
        for path in sorted(folder.glob('*.samples.csv')):
            seed=int(path.stem.split('seed-')[1].split('.')[0]); source=rows(path)
            ep=next(p for p in eventfiles if f'seed-{seed}.' in p.name); events=rows(ep)
            arr=lambda key: np.array([float(r[key]) for r in source])
            received=arr('receive_seconds'); source_time=arr('source_seconds')
            good=np.array([r['validity']=='Valid' for r in source])
            raw=np.column_stack((arr('raw_x'),arr('raw_y')))
            prefix=COLUMNS[kind]; out=np.column_stack((arr(prefix+'_x'),arr(prefix+'_y')))
            take=good & np.isfinite(out).all(axis=1) & ((received-received[0])>=5)
            rawvalid=raw[good]; common={k:filt(rawvalid,k) for k in FILTERS}
            takevalid=(received[good]-received[0])>=5
            m=metrics(raw[take],out[take]); off={k:metrics(rawvalid[takevalid],v[takevalid]) for k,v in common.items()}
            # Logged filters may already be warm at trial start; fit recurrence after 5 s.
            comparison=common[kind][takevalid]-out[good][takevalid]
            fl,fr,rl,rr=[arr(key) for key in ['front_left','front_right','rear_left','rear_right']]
            total=fl+fr+rl+rr
            calculated=np.column_stack(((fr+rr-fl-rl)/total,(fl+fr-rl-rr)/total))
            cop_rmse=float(np.sqrt(np.mean((calculated[good]-raw[good])**2)))
            hits=[e for e in events if e['event_type']=='hit']; misses=[e for e in events if e['event_type']=='miss']
            spawns=[e for e in events if e['event_type']=='spawn']; complete=[e for e in events if e['event_type']=='round_complete']
            assert len(complete)==1
            assert int(complete[0]['score'])==len(hits) and int(complete[0]['misses'])==len(misses)
            assert len({e['balloon_id'] for e in hits+misses})==len(hits)+len(misses)
            assert all(e['sequence_mode']=='FixedSeeded' for e in events)
            phase=[]
            for i in range(3):
                hs=sum(min(2,int(float(e['monotonic_seconds'])//30))==i for e in hits)
                ms=sum(min(2,int(float(e['monotonic_seconds'])//30))==i for e in misses)
                phase.append({'hits':hs,'misses':ms,'rate':100*hs/(hs+ms)})
            freqs,powerx=psd(raw[take,0]); _,powery=psd(raw[take,1])
            source_dt=np.diff(source_time); recv_dt=np.diff(received); seq=arr('sequence')
            r=dict(member=member,filter=kind,seed=seed,samples=len(source),valid=int(good.sum()),hits=len(hits),misses=len(misses),
                spawned=len(spawns),inflight=len(spawns)-len(hits)-len(misses),hit_rate=100*len(hits)/(len(hits)+len(misses)),
                phase=phase,metrics=m,offline=off,rmse_refit=float(np.sqrt(np.mean(comparison**2))),cop_rmse=cop_rmse,
                receive_duration=float(received[-1]-received[0]),round_duration=float(complete[0]['monotonic_seconds']),
                sample_rate=float((len(received)-1)/(received[-1]-received[0])),source_duration=float(source_time[-1]-source_time[0]),
                interval_mean=float(recv_dt.mean()),interval_median=float(np.median(recv_dt)),interval_sd=float(np.std(recv_dt)),
                source_interval_median=float(np.median(source_dt)),source_interval_mean=float(source_dt.mean()),
                receive_quantiles=np.percentile(recv_dt,[5,50,95]).tolist(),sequence_gaps=int(np.maximum(np.diff(seq)-1,0).sum()),
                delta_min=float(arr('delta_seconds').min()),delta_max=float(arr('delta_seconds').max()),
                psd_shares={'ML':{str(f):spectral_share(freqs,powerx,f) for f in [.5,1]},
                            'AP':{str(f):spectral_share(freqs,powery,f) for f in [.5,1]}})
            results.append(r); arrays[(member,seed)]={'raw':raw,'out':out,'t':received-received[0], 'good':good,'common':common,
                'frequency':freqs,'psdx':powerx,'psdy':powery,'events':events}
            for p in [path,ep]: manifest.append({'file':str(p.relative_to(DATA)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
    summaries={}
    for member,kind in MEMBERS:
        rs=[r for r in results if r['member']==member]
        totals={key:sum(r[key] for r in rs) for key in ['samples','valid','hits','misses','spawned','inflight','sequence_gaps']}
        totals['hit_rate']=100*totals['hits']/(totals['hits']+totals['misses'])
        totals['jitter']=float(np.mean([r['metrics']['jitter'] for r in rs]))
        totals['lag_ms']=float(np.mean([r['metrics']['lag_ms'] for r in rs]))
        totals['interpolated_lag_ms']=float(np.mean([r['metrics']['interpolated_lag_ms'] for r in rs]))
        totals['lag_values']=[v for r in rs for v in r['metrics']['lag_axes']]
        totals['rmse_refit_max']=max(r['rmse_refit'] for r in rs)
        totals['phase']=[]
        for i in range(3):
            h=sum(r['phase'][i]['hits'] for r in rs); m=sum(r['phase'][i]['misses'] for r in rs)
            totals['phase'].append({'hits':h,'misses':m,'rate':100*h/(h+m)})
        summaries[member]=totals
    common_summary={kind:{'jitter':float(np.mean([r['offline'][kind]['jitter'] for r in results])),
                           'lag_ms':float(np.mean([r['offline'][kind]['lag_ms'] for r in results])),
                           'interpolated_lag_ms':float(np.mean([r['offline'][kind]['interpolated_lag_ms'] for r in results]))} for kind in FILTERS}
    theory={}
    ff=np.linspace(.00001,9.99999,200000)
    for kind in FILTERS[1:]:
        gains=np.abs(response(kind,ff)); below=np.flatnonzero(gains<=1/np.sqrt(2))
        theory[kind]={'gain_05':float(abs(response(kind,.5))),'gain_1':float(abs(response(kind,1))),
                     'gain_5':float(abs(response(kind,5))),'delay_05_ms':group_ms(kind),
                     'cutoff_hz':float(ff[below[0]]) if len(below) else None}
    spectrum={axis:{str(f):float(np.mean([r['psd_shares'][axis][str(f)] for r in results])) for f in [.5,1]} for axis in ['ML','AP']}
    # The log records actual spawn choices; compare geometry/speed, not just shared seed.
    matched=[]
    for seed in [104729,130363,155921,196613]:
        signatures=[]
        for member,_ in MEMBERS:
            es=arrays[(member,seed)]['events']; signatures.append([(e['x'],e['y'],e['z'],e['speed']) for e in es if e['event_type']=='spawn'])
        prefix=0
        for spawn in zip(*signatures):
            if all(x==spawn[0] for x in spawn): prefix+=1
            else: break
        matched.append({'seed':seed,'identical_initial_spawns':prefix,'total_spawns':len(signatures[0])})
    acquisition={}
    for member,_ in MEMBERS:
        rawfile=DATA/'Raw WIi board'/f'{member}0.txt'
        times=[]
        for line in rawfile.read_text(encoding='utf-8-sig').splitlines()[1:]:
            try: times.append(float(line.split()[0]))
            except (ValueError,IndexError): pass
        times=np.array(times); diffs=np.diff(times)
        acquisition[member]={'rows':len(times),'measured_rate':float((len(times)-1)/(times[-1]-times[0])),
            'interval_quantiles':np.percentile(diffs,[5,50,95]).tolist(),'duration':float(times[-1]-times[0])}
        manifest.append({'file':str(rawfile.relative_to(DATA)),'sha256':hashlib.sha256(rawfile.read_bytes()).hexdigest()})
    payload={'rounds':results,'members':summaries,'common_input':common_summary,'theory':theory,'spectrum':spectrum,'matched_sequences':matched,'recorder_acquisition':acquisition,
             'manifest':manifest,'method':{'sample_rate_hz':FS,'discard_seconds':5,'lag_search_samples':[-10,10],'lag_method':'demeaned globally energy-normalized correlation; integer peak and three-point parabolic interpolation; average across axes and rounds','welch_window':'periodic Hann','welch_segment_samples':256,'welch_overlap_samples':128,'welch_detrend':'subtract each segment mean','power_integration':'trapezoid with interpolated cutoff boundary'}}
    (ROOT/'calculations.json').write_text(json.dumps(payload,indent=2),encoding='utf-8')
    np.savez(ROOT/'series.npz',**{f'{m}_{seed}_{key}':v for (m,seed),a in arrays.items() for key,v in a.items() if key in ['raw','out','t','good','frequency','psdx','psdy']})
    print(json.dumps({k:v for k,v in payload.items() if k not in ['rounds','manifest']},indent=2))
    return payload,arrays

if __name__=='__main__':
    ROOT.mkdir(parents=True,exist_ok=True)
    inspect()
