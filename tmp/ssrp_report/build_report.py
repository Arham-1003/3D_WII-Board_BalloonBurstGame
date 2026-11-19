"""Author a corrected Word report from audited recordings and calculation outputs."""
from pathlib import Path
import json, math
from decimal import Decimal, ROUND_HALF_UP
import numpy as np
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from analyse import ROOT, MEMBERS, butter

OUT=ROOT.parents[1]/'output'/'reports'
OUT.mkdir(parents=True,exist_ok=True)
DEST=OUT/'SSRP Project Report Revised.docx'
D=json.loads((ROOT/'calculations.json').read_text())
TESTS=json.loads((ROOT/'synthetic_tests.json').read_text())
DISPLAY={'Median':'Median','MovingAverage':'Moving average','Butterworth':'Butterworth','AlphaBeta':'Alpha beta'}
doc=Document()
sec=doc.sections[0]
sec.page_width=Inches(8.5); sec.page_height=Inches(11)
sec.top_margin=sec.bottom_margin=Inches(.68)
sec.left_margin=sec.right_margin=Inches(.78)
sec.footer_distance=Inches(.27)
for name in ['Normal','Title','Subtitle','Heading 1','Heading 2','Heading 3','Caption']:
    st=doc.styles[name]; st.font.name='Calibri';st.font.color.rgb=RGBColor(0,0,0)
    st.paragraph_format.space_after=Pt(6)
normal=doc.styles['Normal']; normal.font.size=Pt(11); normal.paragraph_format.line_spacing=1.06
normal.paragraph_format.widow_control=True
for name,size in [('Title',25),('Subtitle',12),('Heading 1',17),('Heading 2',12.5),('Heading 3',11.5),('Caption',9.5)]:
    doc.styles[name].font.size=Pt(size)
for name in ['Heading 1','Heading 2','Heading 3']:
    doc.styles[name].font.bold=True
    doc.styles[name].paragraph_format.keep_with_next=True
    doc.styles[name].paragraph_format.space_before=Pt(10)
doc.styles['Title'].font.bold=True
doc.styles['Caption'].font.italic=False
doc.styles['Caption'].font.bold=False
# Word's bundled template may include a coloured paragraph border under Title.
for border in list(doc.styles.element.xpath('//w:pBdr')):
    border.getparent().remove(border)
doc.core_properties.title='Centre of Pressure Filtering for a Wii Balance Board Balloon Game'
doc.core_properties.subject='SSRP experiment and independently recalculated signal processing evaluation'
doc.core_properties.author='Dharti Prakash Parmar; Arham Gandhi; Alka Kushwaha; Akshat Shah'

fp=sec.footer.paragraphs[0];fp.alignment=WD_ALIGN_PARAGRAPH.CENTER
r=fp.add_run();r.font.size=Pt(9)
field=OxmlElement('w:fldSimple');field.set(qn('w:instr'),'PAGE');r._r.addnext(field)

def p(text='',boldlead=None):
    x=doc.add_paragraph()
    if boldlead and text.startswith(boldlead):x.add_run(boldlead).bold=True;x.add_run(text[len(boldlead):])
    else:x.add_run(text)
    return x

def h(text,level=2):return doc.add_heading(text,level)
def page(title):
    head=h(title,1);head.paragraph_format.page_break_before=True
def bullet(text):return doc.add_paragraph(text,style='List Bullet')
def caption(text):
    x=doc.add_paragraph(text,style='Caption');x.paragraph_format.keep_with_next=True;return x
def note(text):
    x=p(text);x.paragraph_format.space_after=Pt(7)
    for r in x.runs:r.font.size=Pt(9.5)
    return x
def equation(*parts):
    para=doc.add_paragraph();para.alignment=WD_ALIGN_PARAGRAPH.CENTER
    para.paragraph_format.space_before=Pt(2);para.paragraph_format.space_after=Pt(7)
    om=OxmlElement('m:oMath')
    def run(text):
        r=OxmlElement('m:r');pr=OxmlElement('m:rPr');s=OxmlElement('m:sty');s.set(qn('m:val'),'p');pr.append(s);r.append(pr)
        t=OxmlElement('m:t');t.text=text;r.append(t);return r
    for part in parts:
        if isinstance(part,tuple):
            f=OxmlElement('m:f');num=OxmlElement('m:num');den=OxmlElement('m:den')
            num.append(run(part[0]) if isinstance(part[0],str) else part[0])
            den.append(run(part[1]) if isinstance(part[1],str) else part[1])
            f.append(num);f.append(den);om.append(f)
        else:om.append(run(part))
    para._p.append(om);return para

def integral(upper):
    node=OxmlElement('m:nary');props=OxmlElement('m:naryPr')
    char=OxmlElement('m:chr');char.set(qn('m:val'),'∫');props.append(char)
    loc=OxmlElement('m:limLoc');loc.set(qn('m:val'),'subSup');props.append(loc);node.append(props)
    for name,value in [('sub','0'),('sup',upper),('e','S(f) df')]:
        el=OxmlElement('m:'+name);r=OxmlElement('m:r');t=OxmlElement('m:t');t.text=value;r.append(t);el.append(r);node.append(el)
    return node

def table(headers,data,widths=None,size=10):
    t=doc.add_table(rows=1,cols=len(headers));t.alignment=WD_TABLE_ALIGNMENT.CENTER;t.autofit=False
    if widths:
        for c,w in zip(t.columns,widths):c.width=Inches(w)
    allrows=[headers]+[[str(v) for v in row] for row in data]
    for i,row in enumerate(allrows):
        cells=t.rows[0].cells if i==0 else t.add_row().cells
        if widths:
            for c,w in zip(cells,widths):c.width=Inches(w)
        trpr=t.rows[i]._tr.get_or_add_trPr();cant=OxmlElement('w:cantSplit');trpr.append(cant)
        if i==0:
            repeat=OxmlElement('w:tblHeader');trpr.append(repeat)
        for j,(cell,text) in enumerate(zip(cells,row)):
            cell.text=text;cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
            pr=cell._tc.get_or_add_tcPr();sh=OxmlElement('w:shd');sh.set(qn('w:fill'),'263D50' if i==0 else ('F0F4F7' if i%2==0 else 'FFFFFF'));pr.append(sh)
            mar=OxmlElement('w:tcMar')
            for key,v in [('top','75'),('bottom','75'),('left','90'),('right','90')]:
                e=OxmlElement('w:'+key);e.set(qn('w:w'),v);e.set(qn('w:type'),'dxa');mar.append(e)
            pr.append(mar)
            borders=OxmlElement('w:tcBorders')
            for key in ['top','left','bottom','right']:
                e=OxmlElement('w:'+key);e.set(qn('w:val'),'single');e.set(qn('w:sz'),'4');e.set(qn('w:color'),'D9D9D9');borders.append(e)
            pr.append(borders)
            for para in cell.paragraphs:
                para.paragraph_format.space_after=Pt(0);para.paragraph_format.line_spacing=1.02
                para.alignment=WD_ALIGN_PARAGRAPH.LEFT if j==0 else WD_ALIGN_PARAGRAPH.CENTER
                for r in para.runs:
                    r.font.size=Pt(size);r.font.bold=i==0;r.font.color.rgb=RGBColor.from_string('FFFFFF' if i==0 else '000000')
    p('').paragraph_format.space_after=Pt(1)
    return t

def fig(name,cap,width=6.85):
    para=doc.add_paragraph();para.alignment=WD_ALIGN_PARAGRAPH.CENTER
    para.paragraph_format.keep_with_next=True
    run=para.add_run();run.add_picture(str(ROOT/'figures'/f'{name}.png'),width=Inches(width))
    for dp in run._r.xpath('.//wp:docPr'):dp.set('descr',cap)
    c=doc.add_paragraph(cap,style='Caption');c.paragraph_format.space_after=Pt(8)

def fmt(v,n=1):
    return str(Decimal(str(v)).quantize(Decimal(1).scaleb(-n),rounding=ROUND_HALF_UP))

# 1 Title and abstract
doc.add_paragraph('Centre of Pressure Filtering for a Wii Balance Board Balloon Game',style='Title')
doc.add_paragraph('SSRP Project 3   Experimental report',style='Subtitle')
p('Four causal filters were evaluated in a body controlled balloon task and by replaying the same recorded inputs through every filter. The experiment demonstrates a working acquisition and game pipeline, while separating measured behaviour from theoretical predictions and limitations of the study design.')
table(['Team member','Student ID','Assigned experimental filter'],[
    ['Dharti Prakash Parmar','25110228','Alpha beta'],['Arham Gandhi','25110045','Butterworth low pass'],
    ['Alka Kushwaha','25110025','Moving average'],['Akshat Shah','25110019','Median spike']], [2.45,1.1,3.39],10.5)
h('Abstract')
p('A Nintendo Wii Balance Board supplied four corner loads to a desktop Unity balloon game. A Python file to UDP bridge transmitted calibrated loads, while Unity calculated centre of pressure (CoP), applied one causal filter and mapped the result to the absolute position of a blue pointer. Four participants each played four nominal 90 second rounds with one assigned filter, producing 16 round datasets and six scheduled minutes per participant. No VR headset was used.')
p('Independent calculations confirmed approximately 20 Hz acquisition. The three sample median and five sample moving average had approximately 47 and 98 ms interpolated signal lag. The Butterworth coefficients were designed for 2.5 Hz at 50 Hz but ran at about 20 Hz, producing an effective 1 Hz cutoff and approximately 233 ms lag. The alpha beta filter had approximately 7 ms interpolated lag; this is a sub-sample estimate, not measured device latency. Common-input replay demonstrated the smoothing versus delay trade-off. Gameplay scores cannot identify the best filter because each participant used a different filter and seeded spawn sequences diverged during play.')
note('Recordings were collected on 5 November 2026 according to the supplied file names. Calculations and technical verification were revised on 10 November 2026. The original recordings were not modified.')

# 2 Objectives and system
page('Objective')
p('The project investigated how causal discrete time filters change CoP movement before it is used for interactive game control. The objectives were to acquire and preserve board measurements, calculate horizontal and vertical CoP, compare smoothing and delay, and relate the filtered signals to observed task outcomes without treating participant differences as filter effects.')
h('Experimental Design',1)
h('System overview')
fig('pipeline','Figure 1. Physical acquisition, local UDP transport, Unity processing and preserved data. Walker supported board connection; the Recorder supplied the text rows consumed by the bridge.')
h('Software and responsibilities')
table(['Component','Role in the experiment'],[
    ['WiiBalanceWalker v0.5','Supported Bluetooth connection to the board'],
    ['Wii Balance Board Recorder v1.0.0','Board calibration and continuously appended measurement file'],
    ['Python 3 bridge','Read complete rows, assign sequence numbers, preserve an audit and send JSON UDP packets'],
    ['Unity 6000.6.6f1 and C#','CoP, validity checks, selected filter, calibration mapping, game and trial logs'],
    ['Independent report analysis','Python 3.12.14 and NumPy 2.3.5; explicit causal recurrences and FFT calculations']], [2.15,4.79],10)
p('The live endpoint was UDP 127.0.0.1 port 5005 on the same computer. The bridge used Python standard-library modules and did not perform the experimental filtering. The four assessed filters ran inside Unity, once per new accepted sample, not once per rendered frame. vJoy was not the live data transport for these UDP trials.')
note('Software source: COP-MAIN-PROJECT, branch main [1]. The precise Python version used during acquisition was not recorded, so the version above refers only to the independent report analysis.')

# 3 acquisition calibration
page('Signal acquisition and calibration')
h('What one sample contained')
p('The Recorder text row contained time, Recorder CoP x and y, four calibrated corner loads, total weight and four raw sensor counts. The bridge transmitted the four calibrated loads with a sequence number and source timestamp. Unity recomputed CoP rather than using the Recorder CoP columns. The Recorder corner loads were labelled in newtons; the game used their ratios, which are dimensionless.')
equation('W = FL + FR + RL + RR')
equation('CoPₓ = ',('(FR + RR) − (FL + RL)','W'),'     CoPᵧ = ',('(FL + FR) − (RL + RR)','W'))
p('FL, FR, RL and RR denote front left, front right, rear left and rear right. The configured convention made right lean positive x and forward lean positive y. Horizontal CoP was mediolateral (ML); vertical game control used anterior posterior (AP) CoP. These are balance ratios, not centimetres. Unity rejected non-finite or negative corner loads and total load below 10.')
h('Board calibration and participant calibration')
p('The team reported zero-load calibration followed by a known 4 kg mass placed centrally in the Recorder. This corrected the board measurement scale and offsets. It was separate from the five-point participant calibration in Unity: centre, comfortable left, right, forward and backward lean.')
p('The implemented Unity capture used the latest valid raw CoP sample at each button press, not a timed average. Directional ranges had to exceed 0.02 CoP units from centre. The filtered signal was centred and divided by the applicable positive or negative range. A per-axis dead zone then removed small movements and continuously remapped the remaining range before clamping to −1 to +1.')
equation('q = ',('filtered CoP − centre','comfortable directional range'))
equation('u = 0 if |q| ≤ d; otherwise u = sign(q) min(1, (|q| − d)/(1 − d))')
p('The source default dead zone was 0.08 on each axis. Participant-specific calibration values and a build identifier were not present in the supplied CSV headers; their exact values cannot be reconstructed from these files. A larger calibrated range reduces the pointer displacement produced by the same lean, so simply asking for a wider lean is not a general calibration fix.')
note('For future trials, average several seconds of quiet stance and repeat comfortable directional holds. This is a recommendation, not a description of the single-sample captures used in the recorded experiment.')

# 4 protocol
page('Experimental protocol and game')
h('Participants and rounds')
p('Four team members participated. Akshat used the median filter, Alka the moving average, Arham the Butterworth filter and Dharti the alpha beta filter. Each participant played four nominal 90 second rounds with the assigned filter throughout. Four rounds provided six scheduled minutes, meeting the five minute minimum. There was no measured unfiltered gameplay baseline and no participant crossed over between filters.')
p('The intended order of seeds was 104729, 130363, 155921 and 196613. The logs record FixedSeeded mode. Each round spawned 44 balloons, not 40. Balloons still active at round completion were neither hits nor misses and were excluded from the resolved-target hit rate.')
h('Game interaction')
p('Leaning moved the blue pointer, not the camera. The calibrated x and y values set absolute pointer position within approximately ±1.6 horizontal and ±1.0 vertical Unity units around a fixed interaction plane. Holding a lean held a position; it did not continuously integrate pointer velocity. A hit occurred when the pointer and balloon trigger colliders touched. A miss occurred when a balloon passed z = 2.75 without being hit.')
table(['Property','Configured value'],[
    ['Spawn coordinates','x −1.55 to 1.55; y 0.75 to 2.55; z 7 to 12'],
    ['Base approach speed','0.70 to 1.15 Unity units per second'],
    ['Nominal phases','0 to 30 s; 30 to 60 s; 60 to 90 s'],
    ['Spawn intervals by phase','2.4 s; 2.1 s; 1.8 s'],
    ['Speed multipliers by phase','0.85; 1.00; 1.10'],
    ['Display','Desktop monitor; pointer, balloons, score, misses, timer and countdown']], [2.25,4.69],10.5)
h('What fixed seeds did and did not guarantee')
p('Seeded random numbers made the initial spawns repeatable, but spawn candidate rejection also depended on currently active balloons. Player hits changed those active balloons and therefore later random-number consumption. Comparing logged spawn coordinates and speeds across participants found only 17, 14, 6 and 5 identical initial spawns respectively for the four seeds. The trials were seeded but were not fully identical target presentations.')
note('Future comparisons should precompute complete target lists independent of gameplay. This report therefore avoids describing the recorded rounds as fully matched sequences.')

# 5 filters
page('Causal filter implementations')
p('Each selected filter processed x and y separately using the same parameters. Causal means that an output used only the present and previous samples. No forward-backward or zero-phase filtering was used for live control or for the comparisons reported here.')
caption('Table 1. Assessed filter parameters and memory')
table(['Filter','Parameters','Structure'],[
    ['Median spike','N = 3 samples','Nonlinear trailing window'],['Moving average','N = 5 samples','FIR with five equal coefficients'],
    ['Butterworth low pass','Order 2; design 2.5 Hz at 50 Hz','Recursive IIR; realised cutoff about 1 Hz at 20 Hz'],
    ['Alpha beta','α = 0.85; β = 0.05','Position and velocity prediction with correction']], [1.7,2.4,2.84],10)
h('Moving average and median')
equation('y[n] = ',('x[n] + x[n−1] + ⋯ + x[n−N+1]','N'))
equation('y[n] = median(x[n], x[n−1], x[n−2]) for N = 3')
p('The finite impulse response (FIR) moving average combines five inputs spanning four intervals, or 200 ms at 20 Hz. Its group delay is two samples, or 100 ms. The median selects the middle of three ordered values; it removes an isolated one-sample spike in a constant baseline but can alter brief genuine movements. For a monotonic sequence after startup, it returns the previous sample. Both implementations use the available samples during startup.')
h('Second order Butterworth')
equation('y[n] = b₀x[n] + b₁x[n−1] + b₂x[n−2] − a₁y[n−1] − a₂y[n−2]')
p('The verified coefficients were b = [0.02008337, 0.04016673, 0.02008337] and a = [1, −1.56101808, 0.64135154]. The highest output delay is two samples, making this a second-order difference equation. This infinite impulse response (IIR) filter reuses earlier outputs, giving it a decaying memory beyond two samples.')
h('Alpha beta predictor')
equation('p⁻[n] = p[n−1] + v[n−1]Δt;     e[n] = x[n] − p⁻[n]')
equation('p[n] = p⁻[n] + αe[n];     v[n] = v[n−1] + (β/Δt)e[n]')
p('The output is p[n]. Initially, position equals the first input and velocity is zero. Unity clamps Δt to 0.001 to 0.25 s. With fixed Δt and gains the position response is linear time-invariant (LTI) after initialization. Its transfer function follows; irregular changes in Δt do not generally preserve this response.')
equation('H(z) = ',('0.85 − 0.80z⁻¹','1 − 1.10z⁻¹ + 0.15z⁻²'))

# 6 timing
page('Timing audit and implementation verification')
h('Measured rate rather than requested rate')
p('Acquisition rate was calculated from the original Recorder time column and independently from Unity receive timestamps. Complete Recorder files gave 19.986 to 19.996 Hz, including intervals outside gameplay. The 16 game logs gave 19.978 to 20.003 Hz. The mean was therefore approximately 20 Hz, corresponding to a 50 ms average sample period, not 50 Hz.')
equation('f̂ₛ = ',('M − 1','t[M−1] − t[0]'),'     T̄ₛ = 1/f̂ₛ')
p('The median interval was about 47 ms, which differed from the 50 ms mean because intervals were not perfectly uniform. Across complete Recorder files, the 5th to 95th percentile range was approximately 44 to 63 ms. Frequency-domain calculations used a nominal 20 Hz sample-index model without resampling. Their frequencies and delays are approximate physical-time interpretations of this mildly irregular stream.')
h('Two separate timing problems')
p('First, the bridge default interpreted the Recorder Time column as milliseconds although the recorded values were seconds. It divided source times by 1000. For example, a Recorder time of 0.049 s became 0.000049 in the audit. Unity source time consequently spanned about 0.09 instead of 90 s per round. Unity then floored most processing intervals at 0.0001 s; the alpha beta filter separately clamped them to 0.001 s. Receive timestamps and original Recorder timestamps were retained for this audit.')
p('Second, Butterworth coefficients were designed using a nominal 50 Hz rate. The coefficients themselves did not use the timestamp interval on each call. Applying them at 20 Hz shifted the physical cutoff to 2.5 × 20/50 = 1.0 Hz. Correcting the bridge time unit alone would not change those coefficients.')
h('Independent reconstruction')
p('Corner-load formulas reproduced logged raw CoP with a maximum per-round RMSE of 2.03 × 10⁻⁸. The recorded selected outputs were independently replayed using the stated recurrences. The first five seconds were excluded when comparing outputs because filter state already existed before round logging began.')
caption('Table 2. Largest per-round reconstruction RMSE across both axes')
table(['Condition','Maximum RMSE','Interpretation'],[
    ['Median','0','Same median values after startup'],['Moving average','7.83 × 10⁻⁷','Agreement within floating-point accumulation effects'],
    ['Butterworth','9.77 × 10⁻⁸','Supports the stated realised coefficients'],['Alpha beta','5.13 × 10⁻⁷','Constant 0.05 s replay closely matches logged position']], [1.45,1.6,3.89],10)
note('These are numerical consistency checks, not independent board accuracy or device latency measurements. For constant intervals, rescaling velocity by Δt explains why alpha beta position output can remain unchanged; this does not validate corrupted source timestamps.')

# 7 metrics first
page('Evaluation metrics for performance and smoothness')
p('A game-recorded value is stored directly by Unity, such as a hit event. A derived metric is calculated afterward from those logs, such as hit rate or jitter ratio. Unless stated otherwise, signal metrics used valid samples after the first five seconds, separately for x and y and each round. Axis results were averaged within each round, then the four round results were averaged per participant.')
h('Resolved target hit rate')
p('Definition and formula. Hit rate was the percentage of resolved balloons that were successfully burst. H is the number of hit events and L the number of miss events.')
equation('Hit rate (%) = 100 × ',('H','H + L'))
p('Calculation and units. Events were counted by type and checked against the final score and miss counters. Overall rates used total hits divided by total resolved targets, not the unweighted average of round percentages. Units were percent; hits and misses were counts.')
p('Significance. A higher rate means better observed success on resolved targets. It is a task outcome, not a pure filter score. It depends on skill, calibration, target presentation and filter behaviour. Balloons still in flight at completion were counted separately and excluded. Their exclusion favours neither a claim of complete exposure nor a causal filter comparison.')
h('Jitter ratio')
p('Definition. Jitter ratio compared the spread of consecutive changes in filtered CoP with the spread of consecutive changes in raw CoP. Let x[n] be raw CoP and y[n] the selected filtered output for one axis.')
equation('Δx[n] = x[n] − x[n−1];     Δy[n] = y[n] − y[n−1]')
equation('J = ',('s(Δy)','s(Δx)'),'     Jround = (JML + JAP)/2')
p('Here s is sample standard deviation with denominator K − 1 for K differences. Differences were computed between adjacent valid retained samples. There were no within-round sequence gaps in the supplied Unity logs. The ratio has no units because numerator and denominator use the same CoP units.')
p('Interpretation. J < 1 indicates less rapid sample-to-sample variation; J = 1 indicates similar variation; J > 1 indicates more variation. For example, J = 0.75 means the standard deviation of successive changes is 25% lower, not that 25% of sensor noise was removed. Genuine corrective movement can also be reduced.')
p('Significance and limits. This is a compact smoothness measure for comparing outputs produced from the same input. It cannot establish accuracy, responsiveness or noise suppression on its own. Strong smoothing may look good by this measure while delaying useful movement. A nearly constant raw signal would also make the ratio unstable; that was not the case in the analysed gameplay segments.')
note('Control standard deviation, samples at the control limit, regional target hit rates and path-length ratio were intentionally excluded from the main evaluation. They were not necessary to explain the selected filter comparisons.')

# 8 lag
page('Evaluation metric for estimated signal lag')
h('Definition and formula')
p('Estimated signal lag described the time shift that best aligned a filtered CoP trace with its raw trace. It was calculated offline, not measured by the game as a dedicated latency test. For each axis and round, both retained signals were mean-subtracted and correlated over overlapping samples.')
equation('C[k] = Σ (x[n] − x̄)(y[n+k] − ȳ)')
p('The sum covers only indices present in both signals. Correlation was divided by the square root of the product of their full-segment energies; this constant normalization does not change the maximizing lag. The search covered −10 to +10 samples, or nominally ±0.5 s. Positive k meant the filtered output followed the raw input.')
equation('k₀ = arg max C[k];     laginteger = 1000 k₀ / fₛ milliseconds')
h('Why the report contains values below 50 ms')
p('A direct peak can occur only at integer offsets such as 0, 50, 100 or 250 ms at 20 Hz. A three-point parabola around that peak estimated the location between samples. With C₋, C₀ and C₊ denoting correlation at k₀ − 1, k₀ and k₀ + 1, the interpolation was:')
equation('δ = ',('C₋ − C₊','2(C₋ − 2C₀ + C₊)'),'     lagestimated = 1000(k₀ + δ)/fₛ')
p('The small correction was bounded to ±0.5 sample and applied only to an interior peak with a nonzero curvature. Estimates were then averaged across axes and rounds. This procedure reproduced the earlier approximately 47, 98, 233 and 7 ms values. The fractional values are model-based estimates; they do not demonstrate millisecond timing precision from a 20 Hz stream.')
h('Significance and limitations')
p('Smaller positive lag means less temporal trailing for that recorded movement. The estimate depends on the movement spectrum, finite record length and the correlation peak shape. A heavily smoothed output is not an exact shifted copy of the input, so one number summarizes rather than completely characterizes its timing.')
p('Signal lag excludes Bluetooth acquisition delay, Recorder buffering, file polling, UDP transport, rendering and display delay. It is not participant reaction time or complete board-to-screen latency. Those require an external synchronized response test. Theoretical group delay at one frequency is a useful consistency check, but is not expected to equal a broadband cross-correlation lag exactly.')
note('Integer peak results are retained alongside interpolated estimates in Table 5 so that the sample resolution remains visible. No claim of agreement to within a few milliseconds is made.')

# 9 theory metrics
page('Frequency response and raw signal spectrum')
h('Frequency response and cutoff')
p('Definition and formula. For an LTI filter, the transfer function gives the output-to-input complex amplitude ratio for a sinusoid. Evaluating z on the unit circle gives its discrete frequency response.')
equation('H(z) = ',('Σ bᵢz⁻ⁱ','1 + Σ aⱼz⁻ʲ'),'     z = exp(j2πf/fₛ);     gain = |H(z)|')
p('Significance. Gain near 1 preserves sinusoidal amplitude; gain below 1 attenuates it; gain above 1 amplifies it. The low-pass −3 dB point is the first frequency at which gain falls to 1/√2, about 0.707 of the DC amplitude. It is a gradual transition, not a boundary beyond which all movement disappears. Tables report gain at 1 and 5 Hz and the cutoff calculated at 20 Hz.')
p('The median is nonlinear, so it has no single input-independent LTI transfer function. Its response was instead evaluated with common step, ramp and isolated-spike inputs. For alpha beta, the LTI calculation assumes fixed gains and fixed Δt after initialization.')
h('Phase and group delay')
equation('τg(ω) = − ',('d arg H(exp(jω))','dω'),' samples;     τg,ms = 1000 τg/fₛ')
p('Group delay describes how a narrow band of nearby frequencies is delayed by filter phase. It was evaluated at 0.5 Hz from a centred numerical phase derivative. A larger value can contribute to temporal trailing of body movement, but it does not measure human reaction time. The moving average has a 100 ms group delay away from its response zeros; other filters vary with frequency [3].')
h('Raw spectrum from Welch averaging')
p('Definition and calculation. An FFT separates a sampled signal into frequency components. Welch averaging estimates power spectral density (PSD) by averaging windowed segment spectra. Each round and axis used 256-sample periodic Hann windows, 128-sample overlap, segment mean removal and a one-sided density at nominal 20 Hz. Frequency-bin spacing was 20/256 = 0.078125 Hz [2].')
equation('Power share below f₀ (%) = 100 × ',(integral('f₀'),integral('10')))
p('Integrals used trapezoids with interpolation at 0.5 or 1 Hz; shares were averaged equally across rounds. PSD units were CoP-ratio squared per Hz and shares were percentages. The spectrum identified where recorded movement power was concentrated. It did not separate intended lean, natural sway and sensor noise. The Nyquist frequency was 10 Hz; the recordings cannot establish absence of aliased higher-frequency components.')

# 10 results overview
page('Results and dataset integrity')
p('The analysis included all 16 completed sample logs and their 16 event logs, grouped by participant and seed. Counts were recalculated from the files rather than copied from the earlier report. Raw Recorder files and bridge audit files were used to check acquisition format and the timestamp-unit problem.')
caption('Table 3. Dataset and gameplay totals across four rounds per member')
table(['Member','Samples','Valid','Spawned','Hits','Misses','In flight'],[
    [m,f"{D['members'][m]['samples']:,}",f"{D['members'][m]['valid']:,}",176,D['members'][m]['hits'],D['members'][m]['misses'],D['members'][m]['inflight']] for m,_ in MEMBERS],
    [1.05,.95,.95,.95,.85,.85,1.34],10)
p('In total, 28,815 Unity sample rows were recorded and 28,814 were flagged valid. There were 704 spawns, 400 hits, 240 misses and 64 balloons still in flight at completion. Each event file contained one completion record, and its counters agreed with the counted hit and miss events. No balloon ID was resolved twice. There were no sequence-number gaps within these Unity round logs; this does not prove that the physical acquisition missed no measurements.')
caption('Table 4. Live gameplay signal measures using each member’s assigned filter')
table(['Member and filter','Hit rate','Jitter ratio','Estimated lag ms'],[
    [m+'  '+{'Median':'Median','MovingAverage':'Moving average','Butterworth':'Butterworth','AlphaBeta':'Alpha beta'}[k],fmt(D['members'][m]['hit_rate'])+'%',fmt(D['members'][m]['jitter'],3),fmt(D['members'][m]['interpolated_lag_ms'],1)] for m,k in MEMBERS],
    [2.8,1.1,1.25,1.79],10.5)
caption('Table 5. Sampling-grid peaks and interpolated lag estimates')
table(['Filter','Mean integer peak ms','Interpolated mean ms','Interpolation across'],[
    [DISPLAY[k],fmt(D['members'][m]['lag_ms'],0),fmt(D['members'][m]['interpolated_lag_ms'],1),'4 rounds × 2 axes'] for m,k in MEMBERS],
    [1.7,1.65,1.7,1.89],10)
h('Event clock discrepancy')
p('Fifteen completion timestamps were within about 0.006 s of 90 s. Akshat’s seed 155921 round ended at 91.040403 s on the event stopwatch. The game timer used Unity game time while the event logger used an unpaused stopwatch, so pausing or a timing interruption could create such a difference; the logs do not identify its cause. The recorded final score was retained.')
note('The event-time breakdown in Table 7 uses 0 to 30 s, 30 to 60 s and 60 s to completion. Its last interval is longer for that round, so it is not an exact reconstruction of all game-time difficulty phases.')

# 11 individual raw results
page('Results for each participant')
caption('Table 6. Independently counted gameplay results per round')
rr=[]
for member,kind in MEMBERS:
    for r in [r for r in D['rounds'] if r['member']==member]:
        rr.append([member,str(r['seed']),r['hits'],r['misses'],fmt(r['hit_rate'])+'%',r['inflight']])
table(['Member','Seed','Hits','Misses','Hit rate','In flight'],rr,[1.12,1.1,.85,.9,1.2,1.77],10)
caption('Table 7. Event-time success breakdown pooled over four rounds')
table(['Member','0 to 30 s','30 to 60 s','60 s to completion'],[
    [m]+[f"{fmt(x['rate'])}% ({x['hits']}/{x['hits']+x['misses']})" for x in D['members'][m]['phase']] for m,_ in MEMBERS],
    [1.1,1.88,1.88,2.08],10)
p('Akshat achieved 110 hits, Alka 90, Arham 83 and Dharti 117. Arham’s resolved-target hit rate decreased across the three event-time intervals, whereas Dharti’s increased again in the final interval. These are descriptive participant outcomes. Increased target density and speed, differences in calibration, learning and fatigue cannot be separated from the assigned filter by this design.')
note('A hit rate denominator is resolved targets, not all spawns. Comparing hit totals alone also ignores the small differences in how many targets had resolved by completion. No regional target or pointer-limit metrics are included.')

# 12 common input theory
page('Common input comparison and theoretical validation')
p('Every filter was independently applied to every valid recorded raw x and y stream: 16 rounds × 2 axes = 32 signals. Each replay began at the first valid logged sample, used the same causal recurrence as the game and was evaluated after five seconds. This is a computational comparison on identical inputs, not an additional participant experiment.')
caption('Table 8. Same-input replay averages across 32 raw signals')
table(['Filter','Jitter ratio','Estimated lag ms'],[
    [DISPLAY[k],fmt(D['common_input'][k]['jitter'],3),fmt(D['common_input'][k]['interpolated_lag_ms'],1)] for k in ['Median','MovingAverage','Butterworth','AlphaBeta']], [2.9,1.8,2.24],10.5)
caption('Table 9. Linear-filter response calculated at nominal 20 Hz')
table(['Filter','Gain 1 Hz','Gain 5 Hz','First −3 dB','Group delay 0.5 Hz'],[
    ['Moving average','0.904','0.200','1.803 Hz','100.0 ms'],['Butterworth as used','0.707','0.025','1.000 Hz','264.0 ms'],
    ['Alpha beta','1.000','0.840','No crossing below 10 Hz','10.2 ms']], [1.65,1.0,1.0,1.65,1.64],10)
h('Interpretation of the common input results')
p('The same-input replay gave a jitter ratio of 0.669 for Butterworth, 0.796 for moving average, 0.983 for median and 0.972 for alpha beta. These values compare filters on the same movements, whereas Table 4 also reflects differences in the four participants’ raw inputs. Both analyses consistently show the largest lag in the realised Butterworth condition.')
p('A filter order or sample window does not by itself determine performance. The Butterworth design is recursive, the moving average has finite sample memory, and the alpha beta predictor includes both position and velocity state. Their different gains and phases explain why equal-looking smoothing can produce different tracking behaviour.')
page('Frequency response validation')
fig('frequency','Figure 2. Calculated amplitude and group delay for the implemented linear filters. The median has no ordinary LTI curve. Moving-average group delay is plotted below its first zero at 4 Hz.',6.85)
h('What the curves establish')
p('The realised Butterworth response is already down to 0.707 amplitude at 1 Hz, while the moving average retains 0.904 and alpha beta about 1.000. At 5 Hz, Butterworth retains 0.025, moving average 0.200 and alpha beta 0.840. These are sinusoidal amplitude ratios, not percentages of sensor noise removed.')
p('The 0.5 Hz group delays of 264.0, 100.0 and 10.2 ms distinguish their phase behaviour. Measured cross-correlation estimates need not equal those single-frequency values because recorded CoP contains many frequencies. The agreement in overall delay scale supports implementation consistency, without claiming that the difference between estimates is a measurement error.')

# 13 traces
page('Recorded input replay examples')
selection=json.loads((ROOT/'trace_selection.json').read_text())
p(f"Figure 3 uses one recorded ML input from Arham’s first round and processes that identical input through all four filters. The displayed {selection['start']} to {selection['end']} s segment has the largest ML standard deviation among eight-second windows beginning at integer seconds 5 to 75. This explicit selection illustrates dynamic movement; Table 8, not this example, summarizes all recordings.")
fig('common_trace','Figure 3. Grey is the same raw CoP in every panel; the coloured curve is a different causal replay output. Horizontal position is receive time relative to the first logged sample. CoP is a dimensionless balance ratio.',6.6)
p('The median follows the input with a short shift on monotonic portions. The moving average rounds short changes and trails them. The realised Butterworth filter changes more gradually and lags reversals. Alpha beta remains close to the measurement. Apparent visual smoothness alone is insufficient to decide which output preserves the desired movement most accurately.')

# 14 synthetic
page('Common step ramp and spike tests')
p('These newly calculated tests used ideal inputs at exactly 20 Hz, not participant recordings. Filters were initialized at zero before a step at 1 s, a one-second ramp, or a unit spike lasting one sample. They explain causality and transient behaviour without confounding participant movements.')
fig('transients','Figure 4. Causal responses to identical ideal step, ramp and spike inputs. Lines connect discrete samples for readability; they do not imply a higher acquisition rate.',5.8)
caption('Table 10. Simulated transient response measures')
table(['Filter','First ≥50% of step','Peak step output','Peak spike output'],[
    [DISPLAY[k],fmt(TESTS[k]['step_half_time_ms'],0)+' ms',fmt(TESTS[k]['step_peak'],3),fmt(TESTS[k]['spike_peak'],3)] for k in ['Median','MovingAverage','Butterworth','AlphaBeta']], [2.0,1.65,1.65,1.64],10)
note('The first step threshold is measured relative to the first step sample. Alpha beta immediately outputs 0.85 and therefore crosses 0.5 with zero additional sample delay; this is not zero device latency. Butterworth peak overshoot was about 4.44%; alpha beta about 0.89%. The median completely removed this isolated ideal spike, not every possible artifact.')

# 15 spectrum
page('Raw spectral results')
fig('spectrum','Figure 5. Welch raw CoP spectra at nominal 20 Hz. Each round spectrum is divided by its integrated power before averaging, giving every round equal weight. The logarithmic axis exposes small high-frequency components.',6.65)
caption('Table 11. Mean raw-signal power shares after the first five seconds')
table(['Axis','Power below 0.5 Hz','Power below 1 Hz'],[
    ['ML horizontal',fmt(D['spectrum']['ML']['0.5']*100,1)+'%',fmt(D['spectrum']['ML']['1']*100,1)+'%'],
    ['AP vertical',fmt(D['spectrum']['AP']['0.5']*100,1)+'%',fmt(D['spectrum']['AP']['1']*100,1)+'%']], [2.4,2.27,2.27],10.5)
h('Interpretation')
p('Most recorded gameplay variation lay at low frequencies on both axes. This supports designing the control path to preserve relatively slow leaning. It does not prove that everything above 1 Hz is noise: corrective movements, natural sway and artifacts can overlap in frequency. Nor does it establish low sensor noise without a labelled stationary reference.')
p('The revised power percentages differ from the earlier 75% and 94% claims because this report uses an explicit, reproducible Welch configuration and interpolates the band boundary before integrating. The revised values are the calculations reported here; the earlier percentages were not retained without a reproducible method.')
h('Relation to smoothness')
p('Taking successive differences emphasizes fast changes. For an LTI filter, the output difference spectrum is weighted by both the filter gain squared and |1 − exp(−jω)|². Consequently, jitter is influenced by the full input spectrum, not just gain at 5 Hz. A moving average also has sidelobes, so it does not uniformly attenuate more strongly at every increasing frequency. The median must be assessed directly because it is nonlinear.')
note('No residual-noise-at-rest, false-movement-per-minute or signal-to-noise ratio was calculated. The continuous recordings may contain pauses, but they do not identify standardized quiet-standing ground-truth intervals.')

# 16 analysis individuals
page('Analysis for each team member')
h('Akshat and the three sample median')
p('Akshat recorded 110 hits out of 161 resolved targets, or 68.3%. The live jitter ratio was 0.991 and the interpolated lag was 47.5 ms; the direct correlation peak was one sample in both axes of every round. These measurements are consistent with a short trailing median on largely continuous motion. They do not prove that the raw sensor contained few artifacts, because the size and type of any artifacts were not independently labelled. In the common spike test the median rejected the entire isolated spike. This demonstrates the mechanism, not a measured gameplay improvement over raw input.')
h('Alka and the five sample moving average')
p('Alka recorded 90 hits out of 160 resolved targets, or 56.3%. The live jitter ratio was 0.746, corresponding to approximately 25.4% less spread in consecutive changes. The 97.9 ms interpolated lag agreed in scale with the two-sample 100 ms theoretical delay. Its common-input jitter ratio was 0.796, showing why different participant inputs must not be used as a pure filter comparison. A smaller window would reduce memory and delay, but also reduce smoothing. There is no evidence here that changing the calibrated lean range alone would improve the score.')
h('Arham and the second order Butterworth')
p('Arham recorded 83 hits out of 159 resolved targets, or 52.2%. The live jitter ratio was 0.720 and the estimated lag was 233.2 ms. On common inputs this condition produced the lowest jitter ratio, 0.669, and the longest estimated lag, 233.5 ms. The reconstructed output and frequency response verify that the realised cutoff was about 1 Hz. The later event-time hit rate was lower, but the experiment cannot establish that lag caused that decline. The result describes the realised filter, not the intended 2.5 Hz design.')
h('Dharti and the alpha beta predictor')
p('Dharti recorded 117 hits out of 160 resolved targets, or 73.1%. The live jitter ratio was 0.973; the direct correlation peak was zero samples and parabolic interpolation gave approximately 7.3 ms. The corrected transfer function gives gain 1.000 at 1 Hz, 0.840 at 5 Hz and group delay 10.2 ms at 0.5 Hz, not the earlier 1.18 gain and 23 ms delay. This filter followed the measurement closely with little smoothing. The ideal step test showed only about 0.89% overshoot. The highest observed hit rate cannot be attributed to the filter independently of Dharti’s skill and calibration.')
h('Why the same input comparison matters')
p('The common-input ranking was consistent with the intended smoothing mechanisms: Butterworth reduced rapid changes most, moving average next, while median and alpha beta changed them less. That is a defensible comparison of signal processing. A ranking of human performance would require each participant to use every filter under equivalent target presentations.')

# 17 conclusion individual
page('Conclusion for each team member')
h('Akshat')
p('The three-sample median gave a short, approximately one-sample delay with little change in the gameplay jitter measure. It was effective against an isolated ideal spike in the separate computational test. Akshat achieved 68.3% resolved-target hit rate. The study supports its use as a lightweight spike-rejection candidate, but does not establish a gameplay advantage over raw input.')
h('Alka')
p('The five-sample moving average provided predictable smoothing and a delay of about 100 ms. Alka achieved 56.3% hit rate. Its sample window clearly illustrates the memory versus responsiveness trade-off. A three-sample alternative should be tested on the same inputs and with the same participant before any performance improvement is claimed.')
h('Arham')
p('The Butterworth trial demonstrated the strongest smoothing and largest signal lag among the tested conditions. Arham achieved 52.2% hit rate. Its effective cutoff was approximately 1 Hz because 50 Hz design coefficients were executed at about 20 Hz. This is a configuration mismatch, not evidence that all second-order Butterworth filters are unsuitable for interactive movement.')
h('Dharti')
p('The alpha beta condition preserved responsiveness with limited smoothing, and Dharti achieved 73.1% hit rate. The corrected theoretical response and the small simulated overshoot are consistent with a high measurement weight. These results motivate a controlled follow-up but do not identify alpha beta as universally superior.')
h('Team conclusion')
p('The project successfully linked physical load measurements to a Unity game through an external Python UDP bridge while preserving raw and derived records. Four members contributed four labelled gameplay datasets with six nominal minutes each. Independent same-input replay and transient tests demonstrate how filter memory, order and frequency response affect smoothness and lag. The findings also expose the importance of correct timestamp units and sample-rate-aware filter design.')
p('The central experimental conclusion is a trade-off, not a winning filter: stronger smoothing can delay meaningful movement. The observed ranking of hit rates follows the opposite ranking of lag, but it remains an association because participant, filter and partly target presentation were confounded. Correcting those limitations would allow a stronger controlled performance comparison.')

# 18 limitations future
page('Study limitations and next experiment')
h('Limits on the present conclusions')
bullet('Only four participants were tested, each with one filter. Four repeated rounds are not four independent participants, and no significance test can remove this confounding.')
bullet('Shared seeds did not preserve identical complete target sequences because target selection depended on active balloons. Later target exposure therefore differed between participants.')
bullet('Timing was irregular around 20 Hz, source timestamps were scaled incorrectly, and the Butterworth design rate did not match acquisition. Frequency tables use the nominal sample-index model rather than an exactly uniform physical-time record.')
bullet('Participant calibration used single samples and its values were not embedded in the provided CSV headers. Quiet-standing noise, end-to-end latency and subjective comfort were not measured in standardized labelled trials.')
bullet('One event clock exceeded 90 seconds by about one second. Event-time bins cannot perfectly recover the corresponding game-time phases, and final in-flight targets were not scored.')
bullet('No VR headset was used. These results concern desktop gameplay and do not establish VR comfort, safety or headset performance.')
h('Recommended follow up')
p('First correct the Recorder time-unit configuration to seconds and measure the current acquisition rate. Either design fixed filter coefficients for that measured rate or explicitly resample to a uniform timeline before filtering. For a 2.5 Hz second-order Butterworth design at 20 Hz, b is approximately [0.09763107, 0.19526215, 0.09763107] and a is [1, −0.94280904, 0.33333333]. This is a proposed condition, not the condition used for the reported results.')
p('Next precompute target lists and have every participant play every filter in a counterbalanced order, with raw input included as a gameplay baseline if feasible. Keep filter, mapping and target settings fixed within each trial. Add labelled quiet stance and repeated comfortable lean transitions so that rest variation and response delay can be studied under known instructions. Use a consistent procedure for all members.')
p('Finally save a metadata record with participant code, calibration values, filter parameters, measured rate, sequence identifier, software revision and clock units. Keep game-time and monotonic event time separately, and record pauses explicitly. These changes would improve reproducibility; they have not been applied to the game or to the original recordings as part of this report revision.')

# 19 reproducibility references
page('Data provenance and references')
h('Dataset organization')
p('The supplementary material contains four participant-labelled datasets. Each participant folder under Filtered data contains four completed samples files and four corresponding events files, identified by seed. The samples files include corner loads, raw CoP, selected filter output, normalized control, validity and source/receive times. The events files contain spawn, hit, miss and completion records. Inactive filter columns contain NaN and must not be treated as additional live filter recordings.')
p('Raw WIi board contains the original continuous Recorder files. Python Bridge contains the audit CSVs written by the bridge. Those continuous files include data outside the measured game rounds. They should not be equated to a standardized calibration or rest experiment without labels.')
h('Independent calculation record')
p('All report tables were regenerated from the 16 sample/event pairs. Pairing used participant and seed rather than exact millisecond filename equality. The selected-filter outputs were replayed using explicit recurrences; no fitted parameter search or noncausal smoothing was used. The report calculation retained the original source files, recorded SHA-256 checksums and stored unrounded numerical outputs before table rounding.')
p('The numerical work used Python 3.12.14 and NumPy 2.3.5. Welch spectra were calculated explicitly from the FFT with the settings stated in the metric definition. SciPy documentation was used as a methodological reference, not as a claim that SciPy was required by the live bridge. Figures were generated from these calculations. The synthetic figures are clearly separated from acquired participant data.')
h('References')
p('[1] Project source. Arham-1003/COP-MAIN-PROJECT, branch main. Relevant implementations: CopCalculator, CopFilters, CopMapper, PointerController, BalloonGameManager, TrialSampleLogger, TrialEventLogger and balance_board_bridge.py. Repository: https://github.com/Arham-1003/COP-MAIN-PROJECT. Source inspected 10 November 2026. The supplied datasets do not identify an acquisition build commit.')
p('[2] SciPy documentation. scipy.signal.welch. Method and parameter definitions for windowed segment averaging and power spectral density. https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.welch.html. Accessed 10 November 2026.')
p('[3] SciPy documentation. scipy.signal.group_delay. Definition of group delay as the negative derivative of filter phase. https://docs.scipy.org/doc/scipy/reference/generated/scipy.signal.group_delay.html. Accessed 10 November 2026.')
p('[4] Supplied experimental records. Data Acquired, participant recordings dated 5 November 2026. Supplementary release should preserve the four member labels and provide the raw Recorder data, bridge audits and completed Unity CSV pairs with the required access permissions.')
note('A supplementary dataset link must be supplied through the course submission system. No public link, consent record, laboratory location or unrecorded protocol detail is invented in this report.')

# 20 changes requested by user
page('Changes made in this revision')
p('This final section records the changes from the earlier report. It is an editorial change summary and is not an additional experimental result.')
bullet('Reorganized the report from the beginning into Objective, Experimental Design, Results, Analysis and Conclusion, with separate analysis and conclusions for all four members.')
bullet('Recalculated all sample counts, valid counts, spawns, hits, misses, in-flight counts, hit rates and event-time breakdowns from the original completed CSV files. Corrected the statement of 40 balloons to 44 spawns per round.')
bullet('Defined each retained metric with its formula, calculation procedure, units, interpretation and limitations. Distinguished game-recorded events from offline derived metrics.')
bullet('Removed regional target hit rates, control standard deviation, control-limit percentages, path-length ratio and filtered-band power ratios from the evaluation.')
bullet('Recomputed jitter and cross-correlation lag independently. Documented the three-point interpolation that reproduces the fractional lag values, while retaining sampling-grid peak values and rejecting claims of millisecond measurement precision.')
bullet('Verified approximately 20 Hz acquisition from both original Recorder files and Unity receive times. Separated the bridge timestamp-unit error from the Butterworth nominal-rate mismatch and attributed the 0.001 s clamp correctly to Unity’s alpha beta implementation.')
bullet('Replaced the incorrect alpha beta gain and group delay values with the transfer function derived from the implemented recurrence: approximately 1.000 at 1 Hz, 0.840 at 5 Hz and 10.2 ms group delay at 0.5 Hz.')
bullet('Independently replayed the stated filters and reported numerical reconstruction errors rather than claiming every output matched exactly. Corrected the realised second-order Butterworth cutoff to approximately 1 Hz at 20 Hz.')
bullet('Recomputed raw spectra with explicit Welch settings on both axes. Revised the below-0.5 Hz and below-1 Hz shares and removed the unsupported inference that low-frequency concentration proves low sensor noise.')
bullet('Added a pipeline diagram, calculated frequency and group delay curves, common-input recorded traces, ideal step/ramp/spike tests and raw PSD figures. Clearly labelled computational tests as distinct from participant measurements.')
bullet('Checked logged target presentations and identified divergence despite shared seeds. Reported the 91.040403 s event-clock discrepancy and qualified the event-time phase breakdown.')
bullet('Removed unsupported causal statements linking filter delay to participant score, corrected the direction of calibration-range effects, and added a controlled follow-up design and data-provenance notes.')
note('The original Word report, raw recordings and Unity game files were left unchanged. This revised document reports the existing experiment honestly; it does not convert the study into a counterbalanced comparison or add unperformed hardware tests.')

doc.save(DEST)
print(DEST)
print('paragraphs',len(doc.paragraphs),'tables',len(doc.tables),'figures',len(doc.inline_shapes))
