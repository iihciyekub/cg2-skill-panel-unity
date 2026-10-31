#!/usr/bin/env python3
"""Validate this standalone Unity asset repository using only Python stdlib."""
from pathlib import Path
from collections import Counter
import json,re,struct
ROOT=Path(__file__).resolve().parents[1]
BASE=ROOT/'Assets/CG2SkillPanel'

def validate():
    files=[p for p in BASE.rglob('*') if p.is_file() and not p.name.endswith('.meta')]
    guids={}
    for path in [p for p in BASE.rglob('*') if p.is_dir()]+[BASE]+files:
        meta=path.with_name(path.name+'.meta')
        assert meta.is_file(),f'Missing meta: {path.relative_to(ROOT)}'
        found=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(),re.M)
        assert found,f'Invalid GUID: {meta}'
        guid=found.group(1)
        assert guid not in guids,f'Duplicate GUID: {meta} and {guids.get(guid)}'
        guids[guid]=meta
    builder=(BASE/'Editor/CG2SkillPanelBuilder.cs').read_text()
    for name in set(re.findall(r'Sprite\("([^"\n]+)"\)',builder)):
        assert (BASE/'Art'/f'{name}.png').is_file(),f'Missing referenced sprite: {name}'
    image=(BASE/'Art/PanelChrome.png').read_bytes()
    assert image.startswith(b'\x89PNG\r\n\x1a\n') and struct.unpack('>II',image[16:24])==(687,403)
    db=json.loads((BASE/'Resources/cg2-skills.json').read_text())
    assert db['schemaVersion']==3 and db['pages']
    count=0;jobs=set();categories=Counter()
    for page in db['pages']:
        jobs.add((page['category'],page['occupation']));seen=set()
        for row in page['skills']:
            count+=1;categories[row['group']]+=1
            assert row['rowIndex'] not in seen,'Duplicate source row'
            seen.add(row['rowIndex'])
            assert re.fullmatch(r'【\d{3}】',row['code']),row
            assert re.fullmatch(r'MP \d+',row['cost']),row
            assert row['kind'] and row['name'],row
            for key in ('npcName','mapName','position','requirements'):
                assert isinstance(row.get('learning',{}).get(key),str),f'Missing learning.{key}'
            assert (BASE/'Art'/f"{row['skin']}.png").exists(),f"Missing row skin: {row['skin']}"
    controller=(BASE/'Runtime/CG2SkillPanelController.cs').read_text()
    assert 'public SkillLearningInfo learning' in controller
    assert 'LearningValue(learn.npcName)' in controller
    assert 'CG2PixelSnapshots' not in builder+controller
    return dict(assetFiles=len(files),guidCount=len(guids),pages=len(db['pages']),occupations=len(jobs),skillRecords=count,groups=dict(categories),unityEditorValidated=False)

if __name__=='__main__':print(json.dumps(validate(),ensure_ascii=False,indent=2))
