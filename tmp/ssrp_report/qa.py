from pathlib import Path
from pdf2image import convert_from_path
from PIL import Image,ImageOps,ImageDraw
from pypdf import PdfReader
import json,hashlib
ROOT=Path(__file__).resolve().parent
OUT=ROOT/'qa_pages';OUT.mkdir(exist_ok=True)
pdf=ROOT/'revised_qa.pdf'
poppler=r'C:\Users\gandh\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\poppler\Library\bin'
previous={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.glob('page-??.png')}
pages=convert_from_path(str(pdf),dpi=140,output_folder=str(OUT),fmt='png',output_file='page',paths_only=True,poppler_path=poppler,thread_count=3)
for i,path in enumerate(pages,1):
    p=Path(path);target=OUT/f'page-{i:02}.png'
    if p!=target:p.replace(target)
for start in range(0,len(pages),6):
    sheet=Image.new('RGB',(900,1250),'#dde2e7');draw=ImageDraw.Draw(sheet)
    for j in range(min(6,len(pages)-start)):
        im=Image.open(OUT/f'page-{start+j+1:02}.png');im.thumbnail((425,570))
        x=15+(j%2)*450;y=25+(j//2)*412
        im.thumbnail((425,380));sheet.paste(im,(x,y));draw.text((x,y+382),f'Page {start+j+1}',fill='black')
    sheet.save(OUT/f'contact-{start//6+1}.png')
text=[]
for i,page in enumerate(PdfReader(str(pdf)).pages,1):
    t=page.extract_text();text.append({'page':i,'text':t,'characters':len(t)})
(ROOT/'rendered_text.json').write_text(json.dumps(text,indent=2,ensure_ascii=False),encoding='utf-8')
print('Rendered',len(pages),'page images')
print('Changed page images:',[i for i in range(1,len(pages)+1) if previous.get(f'page-{i:02}.png')!=hashlib.sha256((OUT/f'page-{i:02}.png').read_bytes()).hexdigest()])
