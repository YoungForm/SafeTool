import { useState } from 'react'

type LoginResponse = { token: string, user: string }

function Login({ onLogin }: { onLogin: (r: LoginResponse) => void }) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const submit = async () => {
    if (!username || !password) { setError('请输入用户名和密码'); return }
    setLoading(true); setError('')
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
      })
      if (!res.ok) {
        let msg = '登录失败'
        try { const t = await res.text(); msg = JSON.parse(t).message || t } catch {}
        throw new Error(msg)
      }
      const data = await res.json() as LoginResponse
      onLogin(data)
    } catch (e: any) { setError(e.message || String(e)) }
    finally { setLoading(false) }
  }

  return (
    <div style={{ maxWidth: 420, margin: '80px auto', padding: 24, border: '1px solid #e5e7eb', borderRadius: 8 }}>
      <h2>登录</h2>
      <label>用户名<br /><input value={username} onChange={e => setUsername(e.target.value)} /></label>
      <br />
      <label>密码<br /><input type="password" value={password} onChange={e => setPassword(e.target.value)} /></label>
      <br />
      <button onClick={submit} disabled={loading} style={{ marginTop: 12 }}>{loading ? '登录中…' : '登录'}</button>
      {error && <p style={{ color: '#b00' }}>{error}</p>}
    </div>
  )
}

function Compliance({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [systemName, setSystemName] = useState('演示系统')
  const [assessor, setAssessor] = useState('')
  const [hazards, setHazards] = useState<string>('机械挤压, 电击')
  const [severity, setSeverity] = useState<number>(3)
  const [frequency, setFrequency] = useState<number>(2)
  const [avoidance, setAvoidance] = useState<number>(2)
  const [measures, setMeasures] = useState('紧急停止与防护罩')

  const [requiredPL, setRequiredPL] = useState('PLc')
  const [architecture, setArchitecture] = useState('Cat3')
  const [dcavg, setDcavg] = useState(0.9)
  const [mttfd, setMttfd] = useState(10000000)
  const [ccf, setCcf] = useState(65)
  const [validated, setValidated] = useState(true)

  const [result, setResult] = useState<any>(null)
  const [reportHtml, setReportHtml] = useState<string>('')

  const checklist = () => ({
    systemName, assessor, projectId,
    iso12100: {
      identifiedHazards: hazards.split(',').map(s => s.trim()).filter(Boolean),
      severity, frequency, avoidance, riskReductionMeasures: measures
    },
    iso13849: {
      requiredPL, architecture, dcavg, mttfd, ccfScore: ccf, validationPerformed: validated
    },
    generalItems: [
      { code: 'AUTH-001', title: '启用用户认证', required: true, completed: true },
      { code: 'AUTH-002', title: '启用多因素认证', required: true, completed: false },
      { code: 'LOG-001', title: '安全日志与审计', required: true, completed: true }
    ]
  })

  const evalNow = async () => {
    setResult(null)
    const res = await fetch('/api/compliance/evaluate', {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
      body: JSON.stringify(checklist())
    })
    const data = await res.json()
    setResult(data)
  }

  const genReport = async () => {
    const res = await fetch('/api/compliance/report', {
      method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
      body: JSON.stringify(checklist())
    })
    const html = await res.text()
    setReportHtml(html)
  }
  const [matrix, setMatrix] = useState<any[]>([])
  const syncMatrix = async () => {
    const r = await fetch('/api/compliance/matrix?projectId=' + encodeURIComponent(projectId), { headers: { 'Authorization': `Bearer ${token}` } })
    setMatrix(await r.json())
  }

  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>合规自检</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <h3>基本信息</h3>
          <label>项目ID<br /><input value={projectId} onChange={e => setProjectId(e.target.value)} /></label><br />
          <label>系统名称<br /><input value={systemName} onChange={e => setSystemName(e.target.value)} /></label><br />
          <label>评估人<br /><input value={assessor} onChange={e => setAssessor(e.target.value)} /></label>
        </div>
        <div>
          <h3>ISO 12100</h3>
          <label>危害<br /><input value={hazards} onChange={e => setHazards(e.target.value)} /></label><br />
          <label>严重度 (1-4)<br /><input type="number" min={1} max={4} value={severity} onChange={e => setSeverity(+e.target.value)} /></label><br />
          <label>频度 (1-4)<br /><input type="number" min={1} max={4} value={frequency} onChange={e => setFrequency(+e.target.value)} /></label><br />
          <label>可避性 (1-4)<br /><input type="number" min={1} max={4} value={avoidance} onChange={e => setAvoidance(+e.target.value)} /></label><br />
          <label>降低措施<br /><input value={measures} onChange={e => setMeasures(e.target.value)} /></label>
        </div>
        <div>
          <h3>ISO 13849-1</h3>
          <label>所需PL<br /><select value={requiredPL} onChange={e => setRequiredPL(e.target.value)}>
            {['PLa','PLb','PLc','PLd','PLe'].map(p => <option key={p} value={p}>{p}</option>)}
          </select></label><br />
          <label>架构<br /><select value={architecture} onChange={e => setArchitecture(e.target.value)}>
            {['B','Cat1','Cat2','Cat3','Cat4'].map(a => <option key={a} value={a}>{a}</option>)}
          </select></label><br />
          <label>DCavg<br /><input type="number" step={0.01} min={0} max={1} value={dcavg} onChange={e => setDcavg(+e.target.value)} /></label><br />
          <label>MTTFd(h)<br /><input type="number" value={mttfd} onChange={e => setMttfd(+e.target.value)} /></label><br />
          <label>CCF分数<br /><input type="number" value={ccf} onChange={e => setCcf(+e.target.value)} /></label><br />
          <label><input type="checkbox" checked={validated} onChange={e => setValidated(e.target.checked)} /> 已完成验证</label>
          <div style={{ marginTop: 8 }}>
            <button onClick={async () => {
              const itemsRes = await fetch('/api/iso13849/ccf/items', { headers: { 'Authorization': `Bearer ${token}` } })
              const items = await itemsRes.json() as Array<{code:string,title:string,score:number}>
              const chosen = await new Promise<string[]|null>(resolve => {
                const div = document.createElement('div'); div.style.padding = '12px'; div.style.background = '#fff'; div.style.border = '1px solid #e5e7eb';
                const close = () => { document.body.removeChild(div); }
                items.forEach(it => {
                  const label = document.createElement('label'); label.style.display = 'block'; label.style.margin = '6px 0';
                  const cb = document.createElement('input'); cb.type = 'checkbox'; cb.value = it.code;
                  label.appendChild(cb); label.appendChild(document.createTextNode(` ${it.title} (+${it.score})`)); div.appendChild(label);
                })
                const ok = document.createElement('button'); ok.textContent = '计算'; ok.onclick = async () => {
                  const codes = Array.from(div.querySelectorAll('input[type=checkbox]')).filter((x:any) => x.checked).map((x:any) => x.value)
                  resolve(codes); close();
                }
                const cancel = document.createElement('button'); cancel.textContent = '取消'; cancel.style.marginLeft = '8px'; cancel.onclick = () => { resolve(null); close(); }
                div.appendChild(ok); div.appendChild(cancel); document.body.appendChild(div);
              })
              if (!chosen) return
              const scoreRes = await fetch('/api/iso13849/ccf/score', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(chosen) })
              const s = await scoreRes.json() as { score: number }
              setCcf(s.score)
            }}>CCF评分助手</button>
          </div>
        </div>
      </div>
      <div style={{ marginTop: 16 }}>
        <button onClick={evalNow}>执行自检</button>
        <button onClick={genReport} style={{ marginLeft: 8 }}>生成报告</button>
        <button onClick={async () => {
          const res = await fetch('/api/compliance/report.pdf', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(checklist()) })
          const blob = await res.blob(); const url = URL.createObjectURL(blob)
          const a = document.createElement('a'); a.href = url; a.download = 'ComplianceReport.pdf'; a.click(); URL.revokeObjectURL(url)
        }} style={{ marginLeft: 8 }}>导出PDF</button>
        <button onClick={syncMatrix} style={{ marginLeft: 8 }}>同步矩阵</button>
      </div>
      {result && (
        <div style={{ marginTop: 16, padding: 12, border: '1px solid #e5e7eb' }}>
          <h3>自检结果</h3>
          <p>{result.summary}</p>
          {result.nonConformities?.length > 0 && <ul>{result.nonConformities.map((n: string) => <li key={n}>{n}</li>)}</ul>}
        </div>
      )}
      {reportHtml && (
        <div style={{ marginTop: 16 }}>
          <h3>报告预览</h3>
          <iframe srcDoc={reportHtml} style={{ width: '100%', height: 400, border: '1px solid #e5e7eb' }} />
        </div>
      )}
      {matrix.length > 0 && (
        <div style={{ marginTop: 12 }}>
          <h3>矩阵摘要（{projectId}）</h3>
          <div style={{ maxHeight: 200, overflow: 'auto', border: '1px solid #e5e7eb' }}>
            {matrix.map((x, i) => (
              <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 0.6fr 1.6fr 1fr 0.7fr 0.6fr 0.6fr 0.6fr', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
                <div>{x.standard}</div>
                <div>{x.clause}</div>
                <div>{x.requirement}</div>
                <div>{x.reference}</div>
                <div>{x.evidenceId}</div>
                <div>{x.result}</div>
                <div>{x.owner}</div>
                <div>{x.due}</div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

export default function App() {
  const [auth, setAuth] = useState<LoginResponse | null>(null)
  const [tab, setTab] = useState<'compliance'|'iec62061'|'library'|'matrix'|'verification'|'modeler'|'interop'|'evidence'|'srs'|'settings'>('compliance')
  if (!auth) return <Login onLogin={setAuth} />
  return (
    <div>
      <div style={{ padding: 12, borderBottom: '1px solid #e5e7eb' }}>
        <button onClick={() => setTab('compliance')} disabled={tab==='compliance'}>合规自检</button>
        <button onClick={() => setTab('iec62061')} disabled={tab==='iec62061'} style={{ marginLeft: 8 }}>IEC 62061</button>
        <button onClick={() => setTab('library')} disabled={tab==='library'} style={{ marginLeft: 8 }}>组件库</button>
        <button onClick={() => setTab('matrix')} disabled={tab==='matrix'} style={{ marginLeft: 8 }}>验证矩阵</button>
        <button onClick={() => setTab('verification')} disabled={tab==='verification'} style={{ marginLeft: 8 }}>验证清单</button>
        <button onClick={() => setTab('evidence')} disabled={tab==='evidence'} style={{ marginLeft: 8 }}>证据库</button>
        <button onClick={() => setTab('interop')} disabled={tab==='interop'} style={{ marginLeft: 8 }}>互通</button>
        <button onClick={() => setTab('modeler')} disabled={tab==='modeler'} style={{ marginLeft: 8 }}>模型器</button>
        <button onClick={() => setTab('srs')} disabled={tab==='srs'} style={{ marginLeft: 8 }}>SRS</button>
        <button onClick={() => setTab('settings')} disabled={tab==='settings'} style={{ marginLeft: 8 }}>设置</button>
      </div>
      {tab === 'compliance' ? <Compliance token={auth.token} /> : tab === 'iec62061' ? <IEC62061 token={auth.token} /> : tab === 'library' ? <Library token={auth.token} /> : tab === 'matrix' ? <Matrix token={auth.token} /> : tab === 'verification' ? <Verification token={auth.token} /> : tab === 'modeler' ? <Modeler token={auth.token} /> : tab === 'interop' ? <Interop token={auth.token} /> : tab === 'evidence' ? <Evidence token={auth.token} /> : tab === 'srs' ? <SRS token={auth.token} /> : <Settings token={auth.token} />}
    </div>
  )
}

function SRS({ token }: { token: string }) {
  const [docId, setDocId] = useState<string>('')
  const [systemName, setSystemName] = useState('演示系统')
  const [safetyFunction, setSafetyFunction] = useState('急停功能')
  const [plr, setPlr] = useState('PLc')
  const [category, setCategory] = useState('Cat3')
  const [dcavg, setDcavg] = useState(0.9)
  const [mttfd, setMttfd] = useState(10000000)
  const [reaction, setReaction] = useState('<=200ms')
  const [safeState, setSafeState] = useState('断电并机械制动')
  const [requirements, setRequirements] = useState<Array<{title:string,desc:string,crit:string,cat:string,mandatory:boolean,clause:string}>>([
    { title: '输入设备冗余', desc: '双通道急停按钮输入，交叉监控', crit: '通道故障检测并失效安全', cat: '输入', mandatory: true, clause: '5.x' },
    { title: '逻辑失效安全', desc: '安全控制器故障进入安全状态', crit: '故障自检触发安全停机', cat: '逻辑', mandatory: true, clause: '6.x' }
  ])
  const [draft, setDraft] = useState('')

  const create = async () => {
    const payload = {
      systemName,
      safetyFunction,
      requiredPLr: plr,
      architectureCategory: category,
      dcavg,
      mttfd,
      reactionTime: reaction,
      safeState,
      requirements: requirements.map(r => ({ title: r.title, description: r.desc, acceptanceCriteria: r.crit, category: r.cat, mandatory: r.mandatory, clauseRef: r.clause }))
    }
    const res = await fetch('/api/srs/create', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(payload) })
    const data = await res.json()
    setDocId(data.id)
  }

  const exportHtml = async () => {
    if (!docId) return
    const res = await fetch(`/api/srs/${docId}/export`, { headers: { 'Authorization': `Bearer ${token}` } })
    const html = await res.text()
    const blob = new Blob([html], { type: 'text/html' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'SRS.html'; a.click(); URL.revokeObjectURL(url)
  }

  const draftSrs = async () => {
    if (!docId) return
    const res = await fetch(`/api/srs/${docId}/draft`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(null) })
    setDraft(await res.text())
  }

  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>SRS（安全需求规格）</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <label>系统名称<br /><input value={systemName} onChange={e => setSystemName(e.target.value)} /></label><br />
          <label>安全功能<br /><input value={safetyFunction} onChange={e => setSafetyFunction(e.target.value)} /></label><br />
          <label>PLr<br /><select value={plr} onChange={e => setPlr(e.target.value)}>{['PLa','PLb','PLc','PLd','PLe'].map(p => <option key={p} value={p}>{p}</option>)}</select></label><br />
          <label>类别<br /><select value={category} onChange={e => setCategory(e.target.value)}>{['B','Cat1','Cat2','Cat3','Cat4'].map(a => <option key={a} value={a}>{a}</option>)}</select></label>
        </div>
        <div>
          <label>DCavg<br /><input type="number" step={0.01} min={0} max={1} value={dcavg} onChange={e => setDcavg(+e.target.value)} /></label><br />
          <label>MTTFd(h)<br /><input type="number" value={mttfd} onChange={e => setMttfd(+e.target.value)} /></label><br />
          <label>反应时间<br /><input value={reaction} onChange={e => setReaction(e.target.value)} /></label><br />
          <label>安全状态<br /><input value={safeState} onChange={e => setSafeState(e.target.value)} /></label>
        </div>
      </div>

      <h3>需求列表</h3>
      {requirements.map((r, i) => (
        <div key={i} style={{ border: '1px solid #e5e7eb', padding: 8, marginBottom: 8 }}>
          <input value={r.title} onChange={e => { const nr=[...requirements]; nr[i].title=e.target.value; setRequirements(nr) }} placeholder="标题" />
          <input value={r.desc} onChange={e => { const nr=[...requirements]; nr[i].desc=e.target.value; setRequirements(nr) }} placeholder="描述" />
          <input value={r.crit} onChange={e => { const nr=[...requirements]; nr[i].crit=e.target.value; setRequirements(nr) }} placeholder="接受准则" />
          <input value={r.clause} onChange={e => { const nr=[...requirements]; nr[i].clause=e.target.value; setRequirements(nr) }} placeholder="条款引用" />
          <label><input type="checkbox" checked={r.mandatory} onChange={e => { const nr=[...requirements]; nr[i].mandatory=e.target.checked; setRequirements(nr) }} /> 必需</label>
        </div>
      ))}
      <button onClick={() => setRequirements([...requirements, { title: '', desc: '', crit: '', cat: '逻辑', clause: '', mandatory: true }])}>添加需求</button>

      <div style={{ marginTop: 12 }}>
        <button onClick={create}>创建 SRS</button>
        <button onClick={exportHtml} style={{ marginLeft: 8 }} disabled={!docId}>导出 HTML</button>
        <button onClick={async () => {
          if (!docId) return; const res = await fetch(`/api/srs/${docId}/export.pdf`, { headers: { 'Authorization': `Bearer ${token}` } })
          const blob = await res.blob(); const url = URL.createObjectURL(blob)
          const a = document.createElement('a'); a.href = url; a.download = 'SRS.pdf'; a.click(); URL.revokeObjectURL(url)
        }} style={{ marginLeft: 8 }} disabled={!docId}>导出 PDF</button>
        <button onClick={draftSrs} style={{ marginLeft: 8 }} disabled={!docId}>AI 草拟</button>
      </div>
      {draft && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{draft}</pre>}
    </div>
  )
}

function Settings({ token }: { token: string }) {
  const [rules, setRules] = useState<Record<string,string>>({})
  const [logs, setLogs] = useState<any[]>([])
  const [filterUser, setFilterUser] = useState('')
  const [filterAction, setFilterAction] = useState('')
  const [skip, setSkip] = useState(0)
  const [take, setTake] = useState(50)
  const load = async () => {
    const r = await fetch('/api/compliance/plr/rules', { headers: { 'Authorization': `Bearer ${token}` } })
    setRules(await r.json())
    const qs = new URLSearchParams({ user: filterUser, action: filterAction, skip: String(skip), take: String(take) })
    const l = await fetch('/api/srs/audit/logs?' + qs.toString(), { headers: { 'Authorization': `Bearer ${token}` } })
    setLogs(await l.json())
  }
  const save = async () => {
    await fetch('/api/compliance/plr/rules', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(rules) })
    await load()
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>设置</h2>
      <button onClick={load}>加载</button>
      <h3>PLr 映射规则</h3>
      {Object.keys(rules).length === 0 ? <p>暂无规则</p> : (
        <div>
          {Object.entries(rules).map(([k, v]) => (
            <div key={k}>
              <span style={{ width: 120, display: 'inline-block' }}>{k}</span>
              <select value={v} onChange={e => setRules({ ...rules, [k]: e.target.value })}>{['PLa','PLb','PLc','PLd','PLe'].map(p => <option key={p} value={p}>{p}</option>)}</select>
            </div>
          ))}
          <button onClick={save} style={{ marginTop: 8 }}>保存</button>
        </div>
      )}
      <h3 style={{ marginTop: 16 }}>审计日志</h3>
      <div style={{ marginBottom: 8 }}>
        <input placeholder="用户" value={filterUser} onChange={e => setFilterUser(e.target.value)} />
        <input placeholder="操作" value={filterAction} onChange={e => setFilterAction(e.target.value)} style={{ marginLeft: 8 }} />
        <input type="number" placeholder="skip" value={skip} onChange={e => setSkip(+e.target.value)} style={{ marginLeft: 8, width: 80 }} />
        <input type="number" placeholder="take" value={take} onChange={e => setTake(+e.target.value)} style={{ marginLeft: 8, width: 80 }} />
        <button onClick={load} style={{ marginLeft: 8 }}>查询</button>
      </div>
      <div style={{ maxHeight: 240, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {logs.map((x, i) => (
          <div key={i} style={{ padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <strong>{x.time}</strong> {x.user} | {x.action} [{x.resource}] — {x.detail}
          </div>
        ))}
      </div>
    </div>
  )
}

function IEC62061({ token }: { token: string }) {
  const [funcId, setFuncId] = useState('SF-TEST-001')
  const [name, setName] = useState('Emergency Stop')
  const [target, setTarget] = useState('SIL2')
  const [t1, setT1] = useState<number>(2000)
  const [t10d, setT10d] = useState<number>(10000)
  const [subsystems, setSubsystems] = useState<Array<{id:string,name:string,architecture:string,components:Array<{id:string,manufacturer:string,model:string,pfhd:number,beta?:number}>}>>([
    { id: 'SUB-1', name: 'Logic', architecture: '1oo2', components: [ { id: 'LOGIC-PLC-SAFETY-002', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }, { id: 'LOGIC-PLC-SAFETY-003', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 } ] }
  ])
  const [result, setResult] = useState<any>(null)
  const [html, setHtml] = useState<string>('')
  const addSubsystem = () => setSubsystems([...subsystems, { id: 'SUB-' + (subsystems.length + 1), name: '', architecture: '1oo1', components: [] }])
  const addComponent = (si: number) => {
    const ns = [...subsystems]
    ns[si].components.push({ id: 'CMP-' + Date.now(), manufacturer: '', model: '', pfhd: 1e-7 })
    setSubsystems(ns)
  }
  const payload = () => ({ id: funcId, name, targetSIL: target, proofTestIntervalT1: t1, missionTimeT10D: t10d, subsystems })
  const [matrix, setMatrix] = useState<any[]>([])
  const syncMatrix = async () => {
    const r = await fetch('/api/compliance/matrix?projectId=' + encodeURIComponent(funcId), { headers: { 'Authorization': `Bearer ${token}` } })
    setMatrix(await r.json())
  }
  const evaluate = async () => {
    const res = await fetch('/api/iec62061/evaluate', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(payload()) })
    setResult(await res.json())
  }
  const preview = async () => {
    const res = await fetch('/api/iec62061/report', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(payload()) })
    setHtml(await res.text())
  }
  const exportPdf = async () => {
    const res = await fetch('/api/iec62061/report.pdf', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(payload()) })
    const blob = await res.blob(); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'IEC62061Report.pdf'; a.click(); URL.revokeObjectURL(url)
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>IEC 62061 评估</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <label>功能ID<br /><input value={funcId} onChange={e => setFuncId(e.target.value)} /></label><br />
          <label>安全功能<br /><input value={name} onChange={e => setName(e.target.value)} /></label><br />
          <label>目标SIL<br /><select value={target} onChange={e => setTarget(e.target.value)}>{['SIL1','SIL2','SIL3'].map(x => <option key={x} value={x}>{x}</option>)}</select></label><br />
          <label>T1<br /><input type="number" value={t1} onChange={e => setT1(+e.target.value)} /></label><br />
          <label>T10D<br /><input type="number" value={t10d} onChange={e => setT10d(+e.target.value)} /></label>
        </div>
        <div>
          <h3>子系统</h3>
          {subsystems.map((s, i) => (
            <div key={s.id} style={{ border: '1px solid #e5e7eb', padding: 8, marginBottom: 8 }}>
              <input placeholder="名称" value={s.name} onChange={e => { const ns=[...subsystems]; ns[i].name=e.target.value; setSubsystems(ns) }} />
              <select value={s.architecture} onChange={e => { const ns=[...subsystems]; ns[i].architecture=e.target.value; setSubsystems(ns) }}>{['1oo1','1oo2','2oo3'].map(a => <option key={a} value={a}>{a}</option>)}</select>
              <div style={{ marginTop: 6 }}>
                {s.components.map((c, j) => (
                  <div key={c.id} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 8, marginBottom: 6 }}>
                    <input placeholder="制造商" value={c.manufacturer} onChange={e => { const ns=[...subsystems]; ns[i].components[j].manufacturer=e.target.value; setSubsystems(ns) }} />
                    <input placeholder="型号" value={c.model} onChange={e => { const ns=[...subsystems]; ns[i].components[j].model=e.target.value; setSubsystems(ns) }} />
                    <input type="number" placeholder="PFHd" value={c.pfhd} onChange={e => { const ns=[...subsystems]; ns[i].components[j].pfhd=+e.target.value; setSubsystems(ns) }} />
                    <input type="number" placeholder="beta" value={c.beta ?? 0} onChange={e => { const ns=[...subsystems]; ns[i].components[j].beta=+e.target.value; setSubsystems(ns) }} />
                    <button onClick={async () => {
                      const r = await fetch('/api/library/components', { headers: { 'Authorization': `Bearer ${token}` } })
                      const items = await r.json()
                      const div = document.createElement('div'); div.style.padding='12px'; div.style.background='#fff'; div.style.border='1px solid #e5e7eb'; div.style.maxHeight='240px'; div.style.overflow='auto'
                      const close = () => { document.body.removeChild(div) }
                      items.forEach((it:any) => {
                        const row = document.createElement('div'); row.style.display='grid'; row.style.gridTemplateColumns='2fr 1fr 1fr 1fr auto'; row.style.gap='6px'
                        row.appendChild(document.createTextNode(it.id))
                        const m = document.createElement('span'); m.textContent = it.manufacturer; row.appendChild(m)
                        const mdl = document.createElement('span'); mdl.textContent = it.model; row.appendChild(mdl)
                        const cat = document.createElement('span'); cat.textContent = it.category; row.appendChild(cat)
                        const btn = document.createElement('button'); btn.textContent='选择'; btn.onclick = () => {
                          const ns=[...subsystems]; ns[i].components[j].manufacturer = it.manufacturer; ns[i].components[j].model = it.model;
                          const pf = Number(it.parameters?.PFHd ?? it.parameters?.pfhd ?? 1e-7);
                          ns[i].components[j].pfhd = isNaN(pf) ? 1e-7 : pf;
                          const b = Number(it.parameters?.beta ?? it.parameters?.Beta ?? ns[i].components[j].beta ?? 0.05);
                          ns[i].components[j].beta = isNaN(b) ? 0.05 : b; setSubsystems(ns); close();
                        }
                        row.appendChild(btn); div.appendChild(row)
                      })
                      const cancel = document.createElement('button'); cancel.textContent='关闭'; cancel.onclick = close; div.appendChild(cancel)
                      document.body.appendChild(div)
                    }}>从库选择</button>
                  </div>
                ))}
                <button onClick={() => addComponent(i)}>添加组件</button>
              </div>
            </div>
          ))}
          <button onClick={addSubsystem}>添加子系统</button>
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <button onClick={evaluate}>执行评估</button>
        <button onClick={preview} style={{ marginLeft: 8 }}>预览报告</button>
        <button onClick={exportPdf} style={{ marginLeft: 8 }}>导出PDF</button>
        <button onClick={syncMatrix} style={{ marginLeft: 8 }}>同步矩阵</button>
      </div>
      {result && (
        <div style={{ marginTop: 12, padding: 12, border: '1px solid #e5e7eb' }}>
          <div>PFHd: {Number(result.pfHd ?? result.PFHd).toExponential(2)}</div>
          <div>SIL: {result.achievedSIL ?? result.AchievedSIL}</div>
          {result.warnings?.length > 0 && <ul>{result.warnings.map((w: string, idx: number) => <li key={idx}>{w}</li>)}</ul>}
        </div>
      )}
      {html && (
        <div style={{ marginTop: 12 }}>
          <h3>报告预览</h3>
          <iframe srcDoc={html} style={{ width: '100%', height: 360, border: '1px solid #e5e7eb' }} />
        </div>
      )}
      {matrix.length > 0 && (
        <div style={{ marginTop: 12 }}>
          <h3>矩阵摘要（{funcId}）</h3>
          <div style={{ maxHeight: 200, overflow: 'auto', border: '1px solid #e5e7eb' }}>
            {matrix.map((x, i) => (
              <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 0.6fr 1.6fr 1fr 0.7fr 0.6fr 0.6fr 0.6fr', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
                <div>{x.standard}</div>
                <div>{x.clause}</div>
                <div>{x.requirement}</div>
                <div>{x.reference}</div>
                <div>{x.evidenceId}</div>
                <div>{x.result}</div>
                <div>{x.owner}</div>
                <div>{x.due}</div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function Library({ token }: { token: string }) {
  const [items, setItems] = useState<any[]>([])
  const [form, setForm] = useState<any>({ id: '', manufacturer: '', model: '', category: 'sensor', parameters: { PFHd: '1e-7' } })
  const load = async () => {
    const r = await fetch('/api/library/components', { headers: { 'Authorization': `Bearer ${token}` } })
    setItems(await r.json())
  }
  const add = async () => {
    await fetch('/api/library/components', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(form) })
    await load()
  }
  const del = async (id: string) => {
    await fetch('/api/library/components/' + id, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } })
    await load()
  }
  const importJson = async (file: File) => {
    const text = await file.text()
    await fetch('/api/library/import', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: text })
    await load()
  }
  const exportJson = async () => {
    const r = await fetch('/api/library/export', { headers: { 'Authorization': `Bearer ${token}` } })
    const t = await r.text()
    const blob = new Blob([t], { type: 'application/json' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'components.json'; a.click(); URL.revokeObjectURL(url)
  }
  const exportCsv = async () => {
    const r = await fetch('/api/library/export.csv', { headers: { 'Authorization': `Bearer ${token}` } })
    const t = await r.text()
    const blob = new Blob([t], { type: 'text/csv' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'components.csv'; a.click(); URL.revokeObjectURL(url)
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>组件库</h2>
      <div style={{ marginBottom: 8 }}>
        <button onClick={load}>加载</button>
        <button onClick={exportJson} style={{ marginLeft: 8 }}>导出</button>
        <button onClick={exportCsv} style={{ marginLeft: 8 }}>导出CSV</button>
        <input type="file" accept="application/json" onChange={e => { const f=e.target.files?.[0]; if (f) importJson(f) }} style={{ marginLeft: 8 }} />
      </div>
      <div style={{ border: '1px solid #e5e7eb', padding: 8 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 8 }}>
          <input placeholder="ID" value={form.id} onChange={e => setForm({ ...form, id: e.target.value })} />
          <input placeholder="制造商" value={form.manufacturer} onChange={e => setForm({ ...form, manufacturer: e.target.value })} />
          <input placeholder="型号" value={form.model} onChange={e => setForm({ ...form, model: e.target.value })} />
          <select value={form.category} onChange={e => setForm({ ...form, category: e.target.value })}>{['sensor','logic','actuator'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        </div>
        <div style={{ marginTop: 6 }}>
          <input placeholder="PFHd" value={form.parameters.PFHd} onChange={e => setForm({ ...form, parameters: { ...form.parameters, PFHd: e.target.value } })} />
          <button onClick={add} style={{ marginLeft: 8 }}>新增</button>
        </div>
      </div>
      <div style={{ marginTop: 12, maxHeight: 360, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {items.map((x, i) => (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: '2fr 1fr 1fr 1fr auto', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.id}</div>
            <div>{x.manufacturer}</div>
            <div>{x.model}</div>
            <div>{x.category}</div>
            <button onClick={() => del(x.id)}>删除</button>
          </div>
        ))}
      </div>
    </div>
  )
}
function Matrix({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [items, setItems] = useState<any[]>([])
  const [entry, setEntry] = useState<any>({ standard: '', clause: '', requirement: '', reference: '', evidenceId: '', result: '符合', owner: '', due: '' })
  const [evidenceMap, setEvidenceMap] = useState<Record<string,string>>({})
  const importCsv = async (file: File) => {
    const text = await file.text()
    await fetch('/api/compliance/matrix/import?projectId=' + encodeURIComponent(projectId), { method: 'POST', headers: { 'Content-Type': 'text/csv', 'Authorization': `Bearer ${token}` }, body: text })
    await load()
  }
  const load = async () => {
    const r = await fetch('/api/compliance/matrix?projectId=' + encodeURIComponent(projectId), { headers: { 'Authorization': `Bearer ${token}` } })
    const arr = await r.json(); setItems(arr)
    const ids = Array.from(new Set(arr.map((x:any) => x.evidenceId).filter((x:any) => !!x)))
    for (const id of ids) {
      try {
        const er = await fetch('/api/evidence/' + encodeURIComponent(id), { headers: { 'Authorization': `Bearer ${token}` } })
        if (!er.ok) continue; const ev = await er.json()
        setEvidenceMap(prev => ({ ...prev, [id]: ev.name }))
      } catch {}
    }
  }
  const add = async () => {
    await fetch('/api/compliance/matrix?projectId=' + encodeURIComponent(projectId), { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(entry) })
    await load()
  }
  const exportCsv = async () => {
    const r = await fetch('/api/compliance/matrix/export?projectId=' + encodeURIComponent(projectId), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    const t = await r.text(); const blob = new Blob([t], { type: 'text/csv' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'matrix.csv'; a.click(); URL.revokeObjectURL(url)
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>验证矩阵（条款→证据→结果）</h2>
      <div>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <button onClick={load} style={{ marginLeft: 8 }}>加载</button>
        <button onClick={exportCsv} style={{ marginLeft: 8 }}>导出CSV</button>
        <label style={{ marginLeft: 8 }}>导入CSV<input type="file" accept="text/csv,.csv" onChange={e => { const f=e.target.files?.[0]; if (f) importCsv(f) }} /></label>
      </div>
      <div style={{ marginTop: 8, border: '1px solid #e5e7eb', padding: 8 }}>
        <input placeholder="标准" value={entry.standard} onChange={e => setEntry({ ...entry, standard: e.target.value })} />
        <input placeholder="条款" value={entry.clause} onChange={e => setEntry({ ...entry, clause: e.target.value })} />
        <input placeholder="要求摘要" value={entry.requirement} onChange={e => setEntry({ ...entry, requirement: e.target.value })} />
        <input placeholder="引用" value={entry.reference} onChange={e => setEntry({ ...entry, reference: e.target.value })} />
        <input placeholder="证据ID" value={entry.evidenceId} onChange={e => setEntry({ ...entry, evidenceId: e.target.value })} />
        <button onClick={async () => {
          const r = await fetch('/api/evidence', { headers: { 'Authorization': `Bearer ${token}` } })
          const items = await r.json()
          const div = document.createElement('div'); div.style.padding='12px'; div.style.background='#fff'; div.style.border='1px solid #e5e7eb'; div.style.maxHeight='240px'; div.style.overflow='auto'
          const close = () => { document.body.removeChild(div) }
          items.forEach((it:any) => {
            const row = document.createElement('div'); row.style.display='grid'; row.style.gridTemplateColumns='2fr 1fr auto'; row.style.gap='6px'
            row.appendChild(document.createTextNode(it.name))
            const id = document.createElement('span'); id.textContent = it.id; row.appendChild(id)
            const btn = document.createElement('button'); btn.textContent='选择'; btn.onclick = () => { setEntry({ ...entry, evidenceId: it.id }); close(); }
            row.appendChild(btn); div.appendChild(row)
          })
          const cancel = document.createElement('button'); cancel.textContent='关闭'; cancel.onclick = close; div.appendChild(cancel)
          document.body.appendChild(div)
        }} style={{ marginLeft: 8 }}>从证据库选择</button>
        <select value={entry.result} onChange={e => setEntry({ ...entry, result: e.target.value })}>{['符合','不符合','需整改'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        <input placeholder="责任人" value={entry.owner} onChange={e => setEntry({ ...entry, owner: e.target.value })} />
        <input placeholder="期限" value={entry.due} onChange={e => setEntry({ ...entry, due: e.target.value })} />
        <button onClick={add} style={{ marginLeft: 8 }}>新增条目</button>
      </div>
      <div style={{ marginTop: 12, maxHeight: 360, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {items.map((x, i) => (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 0.6fr 1.6fr 1fr 0.7fr 0.6fr 0.6fr 0.6fr', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.standard}</div>
            <div>{x.clause}</div>
            <div>{x.requirement}</div>
            <div>{x.reference}</div>
            <div>
              {evidenceMap[x.evidenceId] ?? x.evidenceId}
              {x.evidenceId && <button style={{ marginLeft: 6 }} onClick={async () => {
                const r = await fetch('/api/evidence/' + encodeURIComponent(x.evidenceId) + '/download', { headers: { 'Authorization': `Bearer ${token}` } })
                if (!r.ok) return; const blob = await r.blob(); const url = URL.createObjectURL(blob)
                const a = document.createElement('a'); a.href = url; a.download = 'evidence_' + x.evidenceId; a.click(); URL.revokeObjectURL(url)
              }}>下载</button>}
            </div>
            <div>{x.result}</div>
            <div>{x.owner}</div>
            <div>{x.due}</div>
          </div>
        ))}
      </div>
    </div>
  )
}

function Evidence({ token }: { token: string }) {
  const [items, setItems] = useState<any[]>([])
  const [name, setName] = useState('')
  const [type, setType] = useState('certificate')
  const [note, setNote] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const [resourceType, setResourceType] = useState('function')
  const [resourceId, setResourceId] = useState('SF-TEST-001')
  const [selectedEvidence, setSelectedEvidence] = useState('')
  const [source, setSource] = useState('')
  const [issuer, setIssuer] = useState('')
  const [validUntil, setValidUntil] = useState('')
  const [url, setUrl] = useState('')
  const load = async () => {
    const r = await fetch('/api/evidence', { headers: { 'Authorization': `Bearer ${token}` } })
    setItems(await r.json())
  }
  const upload = async () => {
    const fd = new FormData(); fd.append('name', name); fd.append('type', type); fd.append('note', note); fd.append('source', source); fd.append('issuer', issuer); fd.append('validUntil', validUntil); fd.append('url', url); if (file) fd.append('file', file)
    await fetch('/api/evidence', { method: 'POST', headers: { 'Authorization': `Bearer ${token}` }, body: fd })
    await load()
  }
  const link = async () => {
    if (!selectedEvidence || !resourceType || !resourceId) return
    await fetch('/api/evidence/link?evidenceId=' + encodeURIComponent(selectedEvidence) + '&resourceType=' + encodeURIComponent(resourceType) + '&resourceId=' + encodeURIComponent(resourceId), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    await load()
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>证据库</h2>
      <div>
        <button onClick={load}>加载</button>
      </div>
      <div style={{ marginTop: 8, border: '1px solid #e5e7eb', padding: 8 }}>
        <input placeholder="名称" value={name} onChange={e => setName(e.target.value)} />
        <select value={type} onChange={e => setType(e.target.value)}>{['certificate','report','photo'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        <input placeholder="备注" value={note} onChange={e => setNote(e.target.value)} />
        <input placeholder="来源" value={source} onChange={e => setSource(e.target.value)} />
        <input placeholder="签发机构" value={issuer} onChange={e => setIssuer(e.target.value)} />
        <input type="date" placeholder="有效期至" value={validUntil} onChange={e => setValidUntil(e.target.value)} />
        <input placeholder="链接URL" value={url} onChange={e => setUrl(e.target.value)} />
        <input type="file" onChange={e => setFile(e.target.files?.[0] ?? null)} />
        <button onClick={upload} style={{ marginLeft: 8 }}>上传</button>
      </div>
      <div style={{ marginTop: 8, border: '1px solid #e5e7eb', padding: 8 }}>
        <h3>绑定证据到资源</h3>
        <select value={selectedEvidence} onChange={e => setSelectedEvidence(e.target.value)}>
          <option value="">选择证据</option>
          {items.map((x:any) => <option key={x.id} value={x.id}>{x.name} ({x.id})</option>)}
        </select>
        <select value={resourceType} onChange={e => setResourceType(e.target.value)} style={{ marginLeft: 8 }}>{['function','calculation','checklist'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        <input placeholder="资源ID" value={resourceId} onChange={e => setResourceId(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={link} style={{ marginLeft: 8 }}>绑定</button>
      </div>
      <div style={{ marginTop: 12, maxHeight: 360, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {items.map((x, i) => (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: '1.2fr 0.8fr 1fr 1fr 1fr 1fr 1fr auto', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.name}</div>
            <div>{x.type}</div>
            <div>{x.status}</div>
            <div>{x.filePath}</div>
            <div>{x.source}</div>
            <div>{x.issuer}</div>
            <div>{x.validUntil ? String(x.validUntil).substring(0,10) : ''}</div>
            <div>{x.url}</div>
            <button onClick={async () => {
              const r = await fetch('/api/evidence/' + x.id + '/download', { headers: { 'Authorization': `Bearer ${token}` } })
              if (!r.ok) return
              const blob = await r.blob(); const url = URL.createObjectURL(blob)
              const a = document.createElement('a'); a.href = url; a.download = 'evidence_' + x.id; a.click(); URL.revokeObjectURL(url)
            }}>下载</button>
          </div>
        ))}
      </div>
    </div>
  )
}
function Verification({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [standard, setStandard] = useState<'ISO13849-2'|'IEC60204-1'>('ISO13849-2')
  const [items, setItems] = useState<any[]>([])
  const load = async () => {
    const qs = new URLSearchParams({ projectId, standard })
    const r = await fetch('/api/verification/items?' + qs.toString(), { headers: { 'Authorization': `Bearer ${token}` } })
    setItems(await r.json())
  }
  const seed = async () => {
    const qs = new URLSearchParams({ projectId, standard })
    await fetch('/api/verification/seed?' + qs.toString(), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    await load()
  }
  const save = async (i: number, patch: any) => {
    const body = { ...items[i], ...patch }
    const qs = new URLSearchParams({ projectId, standard })
    await fetch('/api/verification/items?' + qs.toString(), { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    await load()
  }
  const pushToMatrix = async (i: number) => {
    const x = items[i]
    const entry = { standard, clause: x.Clause ?? x.clause ?? '', requirement: x.Title ?? x.title ?? '', reference: x.Code ?? x.code ?? '', evidenceId: x.EvidenceId ?? x.evidenceId ?? '', result: (x.Result ?? x.result ?? 'pending') === 'pass' ? '符合' : (x.Result ?? x.result) === 'fail' ? '不符合' : '需整改', owner: x.Owner ?? x.owner ?? '', due: x.Due ?? x.due ?? '' }
    await fetch('/api/compliance/matrix?projectId=' + encodeURIComponent(projectId), { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(entry) })
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>验证清单（ISO 13849-2 / IEC 60204-1）</h2>
      <div>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <select value={standard} onChange={e => setStandard(e.target.value as any)} style={{ marginLeft: 8 }}>
          <option value="ISO13849-2">ISO13849-2</option>
          <option value="IEC60204-1">IEC60204-1</option>
        </select>
        <button onClick={load} style={{ marginLeft: 8 }}>加载</button>
        <button onClick={seed} style={{ marginLeft: 8 }}>初始化清单</button>
      </div>
      <div style={{ marginTop: 12, maxHeight: 360, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {items.map((x, i) => (
          <div key={x.id ?? i} style={{ display: 'grid', gridTemplateColumns: '0.8fr 1.2fr 1fr 1fr 0.8fr 0.8fr auto', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.code ?? x.Code}</div>
            <div>{x.title ?? x.Title}</div>
            <div>{x.clause ?? x.Clause}</div>
            <div>
              <select value={(x.result ?? x.Result ?? 'pending')} onChange={e => save(i, { result: e.target.value })}>{['pending','pass','fail'].map(s => <option key={s} value={s}>{s}</option>)}</select>
            </div>
            <div>
              <input placeholder="证据ID" value={x.evidenceId ?? x.EvidenceId ?? ''} onChange={e => save(i, { evidenceId: e.target.value })} />
              <button onClick={async () => {
                const r = await fetch('/api/evidence', { headers: { 'Authorization': `Bearer ${token}` } })
                const list = await r.json()
                const div = document.createElement('div'); div.style.padding='12px'; div.style.background='#fff'; div.style.border='1px solid #e5e7eb'; div.style.maxHeight='240px'; div.style.overflow='auto'
                const close = () => { document.body.removeChild(div) }
                list.forEach((it:any) => {
                  const row = document.createElement('div'); row.style.display='grid'; row.style.gridTemplateColumns='2fr 1fr auto'; row.style.gap='6px'
                  row.appendChild(document.createTextNode(it.name))
                  const id = document.createElement('span'); id.textContent = it.id; row.appendChild(id)
                  const btn = document.createElement('button'); btn.textContent='选择'; btn.onclick = () => { save(i, { evidenceId: it.id }); close(); }
                  row.appendChild(btn); div.appendChild(row)
                })
                const cancel = document.createElement('button'); cancel.textContent='关闭'; cancel.onclick = close; div.appendChild(cancel)
                document.body.appendChild(div)
              }} style={{ marginLeft: 4 }}>选择</button>
            </div>
            <div>
              <input placeholder="责任人" value={x.owner ?? x.Owner ?? ''} onChange={e => save(i, { owner: e.target.value })} />
            </div>
            <div>
              <input placeholder="期限" value={x.due ?? x.Due ?? ''} onChange={e => save(i, { due: e.target.value })} />
            </div>
            <div>
              <button onClick={() => pushToMatrix(i)}>推送矩阵</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
function Modeler({ token }: { token: string }) {
  const [func, setFunc] = useState<any>({ id: 'SF-MODEL-001', name: 'Safety Function', standard: 'ISO13849', target: 'PLc', model: { I: [], L: [], O: [] }, options: {} })
  const [items, setItems] = useState<any[]>([])
  const [loaded, setLoaded] = useState(false)
  const loadLib = async () => {
    const r = await fetch('/api/library/components', { headers: { 'Authorization': `Bearer ${token}` } })
    setItems(await r.json()); setLoaded(true)
  }
  const pickDevice = async (channel: 'I'|'L'|'O') => {
    if (!loaded) await loadLib()
    const div = document.createElement('div'); div.style.padding='12px'; div.style.background='#fff'; div.style.border='1px solid #e5e7eb'; div.style.maxHeight='240px'; div.style.overflow='auto'
    const close = () => { document.body.removeChild(div) }
    items.forEach((it:any) => {
      const row = document.createElement('div'); row.style.display='grid'; row.style.gridTemplateColumns='2fr 1fr 1fr auto'; row.style.gap='6px'
      row.appendChild(document.createTextNode(it.id))
      const m = document.createElement('span'); m.textContent = it.manufacturer; row.appendChild(m)
      const mdl = document.createElement('span'); mdl.textContent = it.model; row.appendChild(mdl)
      const btn = document.createElement('button'); btn.textContent='选择'; btn.onclick = () => {
        const d = { Id: it.id, OverrideParams: {} }; const fm = { ...func }
        fm.model[channel] = [...fm.model[channel], d]; setFunc(fm); close();
      }
      row.appendChild(btn); div.appendChild(row)
    })
    const cancel = document.createElement('button'); cancel.textContent='关闭'; cancel.onclick = close; div.appendChild(cancel)
    document.body.appendChild(div)
  }
  const save = async () => {
    await fetch('/api/model/functions', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(func) })
  }
  const [compute, setCompute] = useState<any>(null)
  const runCompute = async () => {
    const r = await fetch('/api/model/compute', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(func) })
    setCompute(await r.json())
  }
  const exportProject = async () => {
    const r = await fetch('/api/model/project', { headers: { 'Authorization': `Bearer ${token}` } })
    const t = await r.text()
    const blob = new Blob([t], { type: 'application/json' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'project.json'; a.click(); URL.revokeObjectURL(url)
  }
  const importProject = async (file: File) => {
    const text = await file.text()
    await fetch('/api/model/project', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: text })
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>安全功能模型器</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <label>ID<br /><input value={func.id} onChange={e => setFunc({ ...func, id: e.target.value })} /></label><br />
          <label>名称<br /><input value={func.name} onChange={e => setFunc({ ...func, name: e.target.value })} /></label><br />
          <label>标准<br /><select value={func.standard} onChange={e => setFunc({ ...func, standard: e.target.value })}>
            {['ISO13849','IEC62061','both'].map(s => <option key={s} value={s}>{s}</option>)}
          </select></label><br />
          <label>目标<br /><input value={func.target} onChange={e => setFunc({ ...func, target: e.target.value })} /></label>
        </div>
        <div>
          <h3>通道设备</h3>
          <div style={{ marginBottom: 8 }}>
            <strong>I</strong> <button onClick={() => pickDevice('I')}>添加设备</button>
            <div>{(func.model.I||[]).map((d:any, i:number) => <span key={i} style={{ marginRight: 6 }}>{d.Id}</span>)}</div>
            <div style={{ marginTop: 6 }}>
              <label>监测策略
                <select value={func.options?.['I.monitor'] ?? 'none'} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['I.monitor']: e.target.value } })} style={{ marginLeft: 8 }}>
                  <option value="none">none</option>
                  <option value="diagnostics">diagnostics</option>
                  <option value="test">test</option>
                </select>
              </label>
              <label style={{ marginLeft: 12 }}>测试周期
                <input value={func.options?.['I.testPeriod'] ?? ''} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['I.testPeriod']: e.target.value } })} style={{ marginLeft: 8, width: 120 }} />
              </label>
              <label style={{ marginLeft: 12 }}>需求率
                <input value={func.options?.['I.demandRate'] ?? ''} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['I.demandRate']: e.target.value } })} style={{ marginLeft: 8, width: 120 }} />
              </label>
            </div>
          </div>
          <div style={{ marginBottom: 8 }}>
            <strong>L</strong> <button onClick={() => pickDevice('L')}>添加设备</button>
            <div>{(func.model.L||[]).map((d:any, i:number) => <span key={i} style={{ marginRight: 6 }}>{d.Id}</span>)}</div>
            <div style={{ marginTop: 6 }}>
              <label>监测策略
                <select value={func.options?.['L.monitor'] ?? 'none'} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['L.monitor']: e.target.value } })} style={{ marginLeft: 8 }}>
                  <option value="none">none</option>
                  <option value="diagnostics">diagnostics</option>
                  <option value="test">test</option>
                </select>
              </label>
              <label style={{ marginLeft: 12 }}>测试周期
                <input value={func.options?.['L.testPeriod'] ?? ''} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['L.testPeriod']: e.target.value } })} style={{ marginLeft: 8, width: 120 }} />
              </label>
              <label style={{ marginLeft: 12 }}>需求率
                <input value={func.options?.['L.demandRate'] ?? ''} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['L.demandRate']: e.target.value } })} style={{ marginLeft: 8, width: 120 }} />
              </label>
            </div>
          </div>
          <div>
            <strong>O</strong> <button onClick={() => pickDevice('O')}>添加设备</button>
            <div>{(func.model.O||[]).map((d:any, i:number) => <span key={i} style={{ marginRight: 6 }}>{d.Id}</span>)}</div>
            <div style={{ marginTop: 6 }}>
              <label>监测策略
                <select value={func.options?.['O.monitor'] ?? 'none'} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['O.monitor']: e.target.value } })} style={{ marginLeft: 8 }}>
                  <option value="none">none</option>
                  <option value="diagnostics">diagnostics</option>
                  <option value="test">test</option>
                </select>
              </label>
              <label style={{ marginLeft: 12 }}>测试周期
                <input value={func.options?.['O.testPeriod'] ?? ''} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['O.testPeriod']: e.target.value } })} style={{ marginLeft: 8, width: 120 }} />
              </label>
              <label style={{ marginLeft: 12 }}>需求率
                <input value={func.options?.['O.demandRate'] ?? ''} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), ['O.demandRate']: e.target.value } })} style={{ marginLeft: 8, width: 120 }} />
              </label>
            </div>
          </div>
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <div style={{ marginBottom: 8 }}>
          <label>AnnexK 方法
            <select value={func.options?.AnnexKMethod ?? 'simplified'} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), AnnexKMethod: e.target.value } })} style={{ marginLeft: 8 }}>
              <option value="simplified">simplified</option>
              <option value="regular">regular</option>
            </select>
          </label>
          <label style={{ marginLeft: 16 }}>
            <input type="checkbox" checked={(func.options?.testEquip ?? 'false')==='true'} onChange={e => setFunc({ ...func, options: { ...(func.options||{}), testEquip: e.target.checked ? 'true' : 'false' } })} /> 测试设备
          </label>
        </div>
        <button onClick={save}>保存函数</button>
        <button onClick={exportProject} style={{ marginLeft: 8 }}>导出项目</button>
        <label style={{ marginLeft: 8 }}>导入项目<input type="file" accept="application/json" onChange={e => { const f=e.target.files?.[0]; if (f) importProject(f) }} /></label>
        <button onClick={runCompute} style={{ marginLeft: 8 }}>计算建议</button>
      </div>
      {compute && (
        <div style={{ marginTop: 12, padding: 12, border: '1px solid #e5e7eb' }}>
          <div>设备数量: {compute.deviceCount}</div>
          <div>冗余通道: {compute.redundant ? '是' : '否'}</div>
          <div>类别建议: {compute.categorySuggestion}</div>
          <div>PFHd汇总: {Number(compute.pfhdSum).toExponential(2)}</div>
          <div>DCavg估算: {compute.dcavgEst}</div>
          <div>AnnexK方法: {compute.method}</div>
          {compute.warnings?.length > 0 && <ul>{compute.warnings.map((w:string,i:number)=><li key={i}>{w}</li>)}</ul>}
        </div>
      )}
    </div>
  )
}
function Interop({ token }: { token: string }) {
  const [functions, setFunctions] = useState<any[]>([])
  const loadFunctions = async () => {
    const r = await fetch('/api/model/functions', { headers: { 'Authorization': `Bearer ${token}` } })
    setFunctions(await r.json())
  }
  const exportProjectModel = async () => {
    const r = await fetch('/api/model/project', { headers: { 'Authorization': `Bearer ${token}` } })
    const t = await r.text(); const blob = new Blob([t], { type: 'application/json' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'project.model.json'; a.click(); URL.revokeObjectURL(url)
  }
  const exportProjectInterop = async () => {
    const payload = { meta: { name: 'Demo', standard: 'both', createdAt: new Date().toISOString(), author: 'user' }, functions: [] }
    const r = await fetch('/api/interop/export?target=project', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(payload) })
    const t = await r.text(); const blob = new Blob([t], { type: 'application/json' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'project.interop.json'; a.click(); URL.revokeObjectURL(url)
  }
  const importProject = async (file: File) => {
    const text = await file.text()
    await fetch('/api/model/project', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: text })
    await loadFunctions()
  }
  const exportLibraryJson = async () => {
    const r = await fetch('/api/library/export', { headers: { 'Authorization': `Bearer ${token}` } })
    const t = await r.text(); const blob = new Blob([t], { type: 'application/json' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'library.json'; a.click(); URL.revokeObjectURL(url)
  }
  const exportLibraryCsv = async () => {
    const r = await fetch('/api/library/export.csv', { headers: { 'Authorization': `Bearer ${token}` } })
    const t = await r.text(); const blob = new Blob([t], { type: 'text/csv' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'library.csv'; a.click(); URL.revokeObjectURL(url)
  }
  const importLibraryJson = async (file: File) => {
    const text = await file.text()
    await fetch('/api/library/import', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: text })
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>互通（项目导入/导出）</h2>
      <div>
        <button onClick={loadFunctions}>加载函数</button>
        <button onClick={exportProjectModel} style={{ marginLeft: 8 }}>导出项目（model）</button>
        <button onClick={exportProjectInterop} style={{ marginLeft: 8 }}>导出项目（interop）</button>
        <label style={{ marginLeft: 8 }}>导入项目<input type="file" accept="application/json" onChange={e => { const f=e.target.files?.[0]; if (f) importProject(f) }} /></label>
      </div>
      <h3 style={{ marginTop: 12 }}>组件库互通</h3>
      <div>
        <button onClick={exportLibraryJson}>导出库（JSON）</button>
        <button onClick={exportLibraryCsv} style={{ marginLeft: 8 }}>导出库（CSV）</button>
        <label style={{ marginLeft: 8 }}>导入库JSON<input type="file" accept="application/json" onChange={e => { const f=e.target.files?.[0]; if (f) importLibraryJson(f) }} /></label>
      </div>
      <div style={{ marginTop: 12, maxHeight: 360, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {functions.map((f, i) => (
          <div key={i} style={{ padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <strong>{f.id}</strong> {f.name} [{f.standard}] → {f.target}
          </div>
        ))}
      </div>
    </div>
  )
}
