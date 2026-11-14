import json, re
from commands import call
from paths import KIT, RUN

proposal = (KIT/'proposed-plan.md').read_text(encoding='utf-8')
bodies = re.findall(r'```markdown\n(.*?)\n```', proposal, re.S)
assert len(bodies) == 2
assert call('task.list', {'view':'all'}, '04-before-save') == [], 'Tasks exist; stop rather than duplicate an uncertain save.'
lookups = call('task.lookups.get', {}, '05-before-save-lookups')
assert {'INVESTIGATION','REQUEST'} <= {t['code'] for t in lookups['taskTypes']}
lists = call('taskList.list', {}, '06-before-save-lists')
assert len(lists)==1 and lists[0]['id']==1 and lists[0]['name']=='Default list'
specs = [
 dict(title='Investigate invoice export timeout (DEMO-17)',taskTypeCode='INVESTIGATION',sourceReference='DEMO-17',activeWaitingForLabel='Maria — failing example for DEMO-17',items=[
  'Check available export logs for timeout details.',
  'Obtain the failing example already requested from Maria.',
  'Reproduce and investigate the timeout when enough evidence is available.']),
 dict(title='Prepare release checklist',taskTypeCode='REQUEST',sourceReference='DEMO-RELEASE',activeWaitingForLabel=None,items=[
  'Agree the release version and deployment procedure.',
  'Define rollback steps.',
  'Define post-deployment verification steps.'])
]
saved=[]
for number,(spec,body) in enumerate(zip(specs,bodies),1):
    payload={k:v for k,v in spec.items() if k!='items'}
    payload.update(taskListId=1,body=body,bodyFormatCode='MARKDOWN',taskPriorityCode='NORMAL',taskSourceCode=None,sourceUrl=None,owner=None,responsible=None,deadline=None,tags=[])
    result=call('task.create',payload,f'10-task-{number}-create')
    task_id=result['id']
    for i,item in enumerate(spec['items'],1):
        call('task.checklist.create',{'taskId':task_id,'text':item},f'11-task-{number}-checklist-{i}')
    detail=call('task.get',{'id':task_id},f'12-task-{number}-get')
    checklist=call('task.checklist.list',{'taskId':task_id},f'13-task-{number}-checklist')
    timeline=call('task.timeline.get',{'taskId':task_id},f'14-task-{number}-timeline')
    for key,value in payload.items():
        if key != 'activeWaitingForLabel': assert detail[key]==value, (key,detail[key],value)
    wait = detail['activeWaitingFor']
    assert (wait['label'] if wait else None) == spec['activeWaitingForLabel']
    assert detail['taskStatusCode']=='ACTIVE'
    assert len(checklist)==3
    assert [c['text'] for c in checklist]==spec['items']
    assert not any(c['isCompleted'] for c in checklist)
    saved.append(dict(task=detail,checklist=checklist,timeline=timeline))
all_tasks=call('task.list',{'view':'all'},'15-final-tasks')
assert len(all_tasks)==2
(RUN/'saved-result.json').write_text(json.dumps(saved,indent=2,ensure_ascii=False),encoding='utf-8')
print(json.dumps(saved,indent=2,ensure_ascii=False))
