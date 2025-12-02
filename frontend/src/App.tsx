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
    systemName, assessor,
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

  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>合规自检</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <h3>基本信息</h3>
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
    </div>
  )
}

export default function App() {
  const [auth, setAuth] = useState<LoginResponse | null>(null)
  const [tab, setTab] = useState<'compliance'|'srs'|'settings'>('compliance')
  if (!auth) return <Login onLogin={setAuth} />
  return (
    <div>
      <div style={{ padding: 12, borderBottom: '1px solid #e5e7eb' }}>
        <button onClick={() => setTab('compliance')} disabled={tab==='compliance'}>合规自检</button>
        <button onClick={() => setTab('srs')} disabled={tab==='srs'} style={{ marginLeft: 8 }}>SRS</button>
        <button onClick={() => setTab('settings')} disabled={tab==='settings'} style={{ marginLeft: 8 }}>设置</button>
      </div>
      {tab === 'compliance' ? <Compliance token={auth.token} /> : tab === 'srs' ? <SRS token={auth.token} /> : <Settings token={auth.token} />}
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
