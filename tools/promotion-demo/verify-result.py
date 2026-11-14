import json,re
from commands import call
from paths import KIT, RUN
before=json.loads((RUN/'saved-result.json').read_text(encoding='utf-8'))
proposal=(KIT/'proposed-plan.md').read_text(encoding='utf-8')
bodies=re.findall(r'```markdown\n(.*?)\n```',proposal,re.S)
results=[]
for i,old in enumerate(before,1):
    task_id = old['task']['id']
    task=call('task.get',{'id':task_id},f'20-final-task-{i}')
    checks=call('task.checklist.list',{'taskId':task_id},f'21-final-checklist-{i}')
    timeline=call('task.timeline.get',{'taskId':task_id},f'22-final-timeline-{i}')
    for key in ['title','taskListId','taskTypeCode','bodyFormatCode','taskPriorityCode','taskStatusCode','taskSourceCode','sourceReference','sourceUrl','owner','responsible','deadline','tags']:
        assert key in task and task[key]==old['task'][key],(i,key)
    assert task['body']==bodies[i-1]
    assert task['taskListId']==1 and task['taskStatusCode']=='ACTIVE' and task['taskPriorityCode']=='NORMAL'
    assert all(task[k] is None for k in ['owner','responsible','deadline','taskSourceCode','sourceUrl'])
    assert checks==old['checklist'] and len(checks)==3 and not any(c['isCompleted'] for c in checks)
    if i==1:
        assert task['activeWaitingFor']['label']=='Maria \u2014 failing example for DEMO-17'
    else: assert task['activeWaitingFor'] is None
    comments=[x for x in timeline if x['kind']=='comment']
    assert len(comments)==(1 if i==1 else 0)
    if comments: assert comments[0]['text']=='Demo review: source notes checked against the saved plan.'
    results.append(dict(task=task,checklist=checks,timeline=timeline))
assert len(call('task.list',{'view':'all'},'23-final-count'))==2
(RUN/'rehearsal-result.json').write_text(json.dumps(results,indent=2,ensure_ascii=False),encoding='utf-8')
print('PASS: two exact task bodies, fields, six unchecked checklist items, waiting label, one review comment, and no duplicates.')
