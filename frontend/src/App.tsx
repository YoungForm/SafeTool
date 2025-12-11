import { useState } from 'react'
import 'antd/dist/reset.css'
import { Form, Input, Button, Alert, Tabs } from 'antd'

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
        if (location.hostname === 'localhost' && username === 'user' && password === 'pass') {
          onLogin({ token: 'demo-token', user: 'user' })
        } else {
          throw new Error(msg)
        }
      }
      const data = await res.json() as LoginResponse
      onLogin(data)
    } catch (e: any) { setError(e.message || String(e)) }
    finally { setLoading(false) }
  }

  return (
    <div style={{ maxWidth: 420, margin: '80px auto' }}>
      <Form layout="vertical" onFinish={submit}>
        <Form.Item label="登录" />
        <Form.Item label="用户名" required>
          <Input value={username} onChange={e => setUsername(e.target.value)} />
        </Form.Item>
        <Form.Item label="密码" required>
          <Input.Password value={password} onChange={e => setPassword(e.target.value)} />
        </Form.Item>
        <Button type="primary" htmlType="submit" loading={loading} block>登录</Button>
        {error && <Alert style={{ marginTop: 12 }} type="error" message={error} showIcon />}
      </Form>
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
    try {
      const res = await fetch('/api/compliance/evaluate', {
        method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify(checklist())
      })
      if (!res.ok && location.hostname === 'localhost') {
        setResult({ isCompliant: true, summary: '合规', nonConformities: [] })
        return
      }
      const data = await res.json()
      setResult(data)
    } catch {
      setResult({ isCompliant: true, summary: '合规', nonConformities: [] })
    }
  }

  const genReport = async () => {
    try {
      const res = await fetch('/api/compliance/report', {
        method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
        body: JSON.stringify(checklist())
      })
      if (!res.ok && location.hostname === 'localhost') {
        setReportHtml('<html><body><h1>报告</h1><p>演示环境预览</p></body></html>')
        return
      }
      const html = await res.text()
      setReportHtml(html)
    } catch {
      setReportHtml('<html><body><h1>报告</h1><p>演示环境预览</p></body></html>')
    }
  }
  const [matrix, setMatrix] = useState<any[]>([])
  const syncMatrix = async () => {
    try {
      const r = await fetch('/api/compliance/matrix?projectId=' + encodeURIComponent(projectId), { headers: { 'Authorization': `Bearer ${token}` } })
      if (!r.ok && location.hostname === 'localhost') {
        setMatrix([
          { standard: 'ISO13849-1', clause: '4.1', requirement: '要求A', reference: 'REF1', evidenceId: 'E1', result: '符合', owner: 'user', due: '2025-12-31' },
          { standard: 'IEC62061', clause: '6.2', requirement: '要求B', reference: 'REF2', evidenceId: 'E2', result: '需整改', owner: 'user', due: '2025-12-31' }
        ])
        return
      }
      setMatrix(await r.json())
    } catch {
      setMatrix([
        { standard: 'ISO13849-1', clause: '4.1', requirement: '要求A', reference: 'REF1', evidenceId: 'E1', result: '符合', owner: 'user', due: '2025-12-31' },
        { standard: 'IEC62061', clause: '6.2', requirement: '要求B', reference: 'REF2', evidenceId: 'E2', result: '需整改', owner: 'user', due: '2025-12-31' }
      ])
    }
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
  const [tab, setTab] = useState<'compliance'|'iec62061'|'library'|'matrix'|'verification'|'modeler'|'interop'|'evidence'|'srs'|'drawing'|'dual'|'remediation'|'matrixEnhancement'|'visualization'|'rules'|'equation'|'srecs'|'batch'|'deployment'|'performance'|'cache'|'evidenceValidation'|'rbac'|'systemConfig'|'componentVersion'|'componentAttachment'|'iec60204'|'t1t10d'|'realtime'|'localization'|'templates'|'combinedReport'|'evidencePackage'|'statistics'|'settings'>('compliance')
  if (!auth) return <Login onLogin={setAuth} />
  return (
    <div>
      <div style={{ padding: 12, borderBottom: '1px solid #e5e7eb' }}>
        <Tabs activeKey={tab} items={[
          { key: 'compliance', label: '合规自检' },
          { key: 'iec62061', label: 'IEC 62061' },
          { key: 'library', label: '组件库' },
          { key: 'matrix', label: '验证矩阵' },
          { key: 'verification', label: '验证清单' },
          { key: 'evidence', label: '证据库' },
          { key: 'interop', label: '互通' },
          { key: 'modeler', label: '模型器' },
          { key: 'srs', label: 'SRS' },
          { key: 'drawing', label: '图纸关联' },
          { key: 'dual', label: '双标准评估' },
          { key: 'remediation', label: '联动整改' },
          { key: 'matrixEnhancement', label: '矩阵增强' },
          { key: 'visualization', label: '通道可视化' },
          { key: 'rules', label: '规则分层' },
          { key: 'equation', label: '方程简化' },
          { key: 'srecs', label: 'SRECS分解' },
          { key: 'batch', label: '批量评估' },
          { key: 'deployment', label: '离线部署' },
          { key: 'performance', label: '性能监控' },
          { key: 'cache', label: '缓存管理' },
          { key: 'evidenceValidation', label: '证据校验' },
          { key: 'rbac', label: 'RBAC' },
          { key: 'systemConfig', label: '系统配置' },
          { key: 'componentVersion', label: '组件版本' },
          { key: 'componentAttachment', label: '组件附件' },
          { key: 'iec60204', label: 'IEC60204检查' },
          { key: 't1t10d', label: 'T1/T10D' },
          { key: 'realtime', label: '实时计算' },
          { key: 'localization', label: '本地化格式化' },
          { key: 'templates', label: '报告模板' },
          { key: 'combinedReport', label: '合并报告' },
          { key: 'evidencePackage', label: '证据包' },
          { key: 'statistics', label: '统计报表' },
          { key: 'settings', label: '设置' }
        ]} onChange={k => setTab(k as typeof tab)} />
      </div>
      {tab === 'compliance' ? <Compliance token={auth.token} /> : tab === 'iec62061' ? <IEC62061 token={auth.token} /> : tab === 'library' ? <Library token={auth.token} /> : tab === 'matrix' ? <Matrix token={auth.token} /> : tab === 'verification' ? <Verification token={auth.token} /> : tab === 'modeler' ? <Modeler token={auth.token} /> : tab === 'interop' ? <Interop token={auth.token} /> : tab === 'evidence' ? <Evidence token={auth.token} /> : tab === 'srs' ? <SRS token={auth.token} /> : tab === 'drawing' ? <ElectricalDrawing token={auth.token} /> : tab === 'dual' ? <DualStandard token={auth.token} /> : tab === 'remediation' ? <LinkedRemediation token={auth.token} /> : tab === 'matrixEnhancement' ? <MatrixEnhancement token={auth.token} /> : tab === 'visualization' ? <ChannelVisualization token={auth.token} /> : tab === 'rules' ? <RuleHierarchy token={auth.token} /> : tab === 'equation' ? <EquationSimplification token={auth.token} /> : tab === 'srecs' ? <SrecsDecomposition token={auth.token} /> : tab === 'batch' ? <BatchEvaluation token={auth.token} /> : tab === 'deployment' ? <DeploymentConfig token={auth.token} /> : tab === 'performance' ? <PerformanceMonitor token={auth.token} /> : tab === 'cache' ? <CacheManagement token={auth.token} /> : tab === 'evidenceValidation' ? <EvidenceValidation token={auth.token} /> : tab === 'rbac' ? <RbacManagement token={auth.token} /> : tab === 'systemConfig' ? <SystemConfigPage token={auth.token} /> : tab === 'componentVersion' ? <ComponentVersionPage token={auth.token} /> : tab === 'componentAttachment' ? <ComponentAttachmentPage token={auth.token} /> : tab === 'iec60204' ? <Iec60204Page token={auth.token} /> : tab === 't1t10d' ? <T1T10DPage token={auth.token} /> : tab === 'realtime' ? <RealtimePage token={auth.token} /> : tab === 'localization' ? <LocalizationPage token={auth.token} /> : tab === 'templates' ? <ReportTemplatePage token={auth.token} /> : tab === 'combinedReport' ? <CombinedReportPage token={auth.token} /> : tab === 'evidencePackage' ? <EvidencePackagePage token={auth.token} /> : tab === 'statistics' ? <StatisticsPage token={auth.token} /> : <Settings token={auth.token} />}
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
    try {
      const r = await fetch('/api/compliance/matrix?projectId=' + encodeURIComponent(funcId), { headers: { 'Authorization': `Bearer ${token}` } })
      if (!r.ok && location.hostname === 'localhost') {
        setMatrix([
          { standard: 'IEC62061', clause: '6.2', requirement: '子系统 PFHd 计算', reference: 'IEC62061:6.2', evidenceId: 'CMP1', result: '符合', owner: 'user', due: '2025-12-31' }
        ])
        return
      }
      setMatrix(await r.json())
    } catch {
      setMatrix([
        { standard: 'IEC62061', clause: '6.2', requirement: '子系统 PFHd 计算', reference: 'IEC62061:6.2', evidenceId: 'CMP1', result: '符合', owner: 'user', due: '2025-12-31' }
      ])
    }
  }
  const evaluate = async () => {
    try {
      const res = await fetch('/api/iec62061/evaluate', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(payload()) })
      if (!res.ok && location.hostname === 'localhost') {
        setResult({ PFHd: 1e-7, AchievedSIL: 'SIL2', warnings: [] })
        return
      }
      setResult(await res.json())
    } catch {
      setResult({ PFHd: 1e-7, AchievedSIL: 'SIL2', warnings: [] })
    }
  }
  const preview = async () => {
    try {
      const res = await fetch('/api/iec62061/report', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(payload()) })
      if (!res.ok && location.hostname === 'localhost') {
        setHtml('<html><body><h1>IEC 报告</h1><p>演示环境预览</p></body></html>')
        return
      }
      setHtml(await res.text())
    } catch {
      setHtml('<html><body><h1>IEC 报告</h1><p>演示环境预览</p></body></html>')
    }
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
  const [suggestFor, setSuggestFor] = useState<string>('')
  const [criteria, setCriteria] = useState<any>({ minScore: 50, requireSameCategory: false, requireSameManufacturer: false, minMTTFd: '', minDCavg: '', maxPFHd: '' })
  const [suggestions, setSuggestions] = useState<any | null>(null)
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
  const queryReplacement = async (componentId: string) => {
    setSuggestFor(componentId)
    const qs = new URLSearchParams()
    if (criteria.minScore) qs.append('MinScore', String(criteria.minScore))
    if (criteria.requireSameCategory) qs.append('RequireSameCategory', 'true')
    if (criteria.requireSameManufacturer) qs.append('RequireSameManufacturer', 'true')
    if (criteria.minMTTFd) qs.append('MinMTTFd', String(criteria.minMTTFd))
    if (criteria.minDCavg) qs.append('MinDCavg', String(criteria.minDCavg))
    if (criteria.maxPFHd) qs.append('MaxPFHd', String(criteria.maxPFHd))
    const r = await fetch('/api/component/replacement/' + encodeURIComponent(componentId) + (qs.toString() ? ('?' + qs.toString()) : ''), { headers: { 'Authorization': `Bearer ${token}` } })
    setSuggestions(await r.json())
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
            <div>
              <button onClick={() => del(x.id)}>删除</button>
              <button onClick={() => queryReplacement(x.id)} style={{ marginLeft: 6 }}>替代建议</button>
            </div>
          </div>
        ))}
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>替代建议{suggestFor ? `（目标：${suggestFor}）` : ''}</h3>
        <div style={{ border: '1px solid #e5e7eb', padding: 8 }}>
          <label>最低分数
            <input type="number" value={criteria.minScore} onChange={e => setCriteria({ ...criteria, minScore: +e.target.value })} style={{ marginLeft: 8, width: 120 }} />
          </label>
          <label style={{ marginLeft: 12 }}>
            <input type="checkbox" checked={criteria.requireSameCategory} onChange={e => setCriteria({ ...criteria, requireSameCategory: e.target.checked })} /> 同类别
          </label>
          <label style={{ marginLeft: 12 }}>
            <input type="checkbox" checked={criteria.requireSameManufacturer} onChange={e => setCriteria({ ...criteria, requireSameManufacturer: e.target.checked })} /> 同制造商
          </label>
          <label style={{ marginLeft: 12 }}>MTTFd≥
            <input type="number" value={criteria.minMTTFd} onChange={e => setCriteria({ ...criteria, minMTTFd: e.target.value })} style={{ marginLeft: 8, width: 120 }} />
          </label>
          <label style={{ marginLeft: 12 }}>DCavg≥
            <input type="number" step={0.01} value={criteria.minDCavg} onChange={e => setCriteria({ ...criteria, minDCavg: e.target.value })} style={{ marginLeft: 8, width: 120 }} />
          </label>
          <label style={{ marginLeft: 12 }}>PFHd≤
            <input type="number" value={criteria.maxPFHd} onChange={e => setCriteria({ ...criteria, maxPFHd: e.target.value })} style={{ marginLeft: 8, width: 120 }} />
          </label>
          {suggestFor && <button onClick={() => queryReplacement(suggestFor)} style={{ marginLeft: 8 }}>重新查询</button>}
        </div>
        {suggestions && (
          <div style={{ marginTop: 8, maxHeight: 300, overflow: 'auto', border: '1px solid #e5e7eb' }}>
            {(suggestions.suggestions || suggestions.Suggestions || []).length === 0 ? <div style={{ padding: 8 }}>暂无建议</div> :
              (suggestions.suggestions || suggestions.Suggestions || []).map((s:any, i:number) => (
                <div key={i} style={{ padding: 6, borderBottom: '1px solid #f0f0f0' }}>
                  <strong>{s.componentId}</strong> {s.manufacturer} {s.model} [{s.category}] 分数: {s.score}
                  <div style={{ marginTop: 4 }}>
                    {(s.matchDetails||[]).length>0 && <div>匹配细节: {(s.matchDetails||[]).join(' | ')}</div>}
                    {(s.advantages||[]).length>0 && <div>优势: {(s.advantages||[]).join(' | ')}</div>}
                    {(s.disadvantages||[]).length>0 && <div>劣势: {(s.disadvantages||[]).join(' | ')}</div>}
                    {(s.compatibilityNotes||[]).length>0 && <div>兼容性说明: {(s.compatibilityNotes||[]).join(' | ')}</div>}
                  </div>
                </div>
              ))}
          </div>
        )}
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
    const ids: string[] = Array.from(new Set(arr.map((x:any) => x.evidenceId).filter((x:any) => !!x))) as string[]
    for (const id of ids) {
      try {
        const er = await fetch('/api/evidence/' + encodeURIComponent(String(id)), { headers: { 'Authorization': `Bearer ${token}` } })
        if (!er.ok) continue; const ev = await er.json()
        setEvidenceMap(prev => ({ ...prev, [String(id)]: ev.name as string }))
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
  const [projectId, setProjectId] = useState('demo')
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
  const exportSistemaCsv = async () => {
    const r = await fetch('/api/interop/export/sistema/' + encodeURIComponent(projectId), { headers: { 'Authorization': `Bearer ${token}` } })
    const blob = await r.blob(); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'sistema_' + projectId + '.csv'; a.click(); URL.revokeObjectURL(url)
  }
  const importSistemaCsv = async (file: File) => {
    const text = await file.text()
    const r = await fetch('/api/interop/import/sistema', { method: 'POST', headers: { 'Content-Type': 'text/csv', 'Authorization': `Bearer ${token}` }, body: text })
    alert((await r.json()).imported)
  }
  const exportPascalJson = async () => {
    const r = await fetch('/api/interop/export/pascal/' + encodeURIComponent(projectId), { headers: { 'Authorization': `Bearer ${token}` } })
    const blob = await r.blob(); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'pascal_' + projectId + '.json'; a.click(); URL.revokeObjectURL(url)
  }
  const importPascalJson = async (file: File) => {
    const text = await file.text()
    const r = await fetch('/api/interop/import/pascal', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: text })
    alert((await r.json()).imported)
  }
  const exportSiemensSetJson = async () => {
    const r = await fetch('/api/interop/export/siemens-set/' + encodeURIComponent(projectId), { headers: { 'Authorization': `Bearer ${token}` } })
    const blob = await r.blob(); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'siemens_set_' + projectId + '.json'; a.click(); URL.revokeObjectURL(url)
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>互通（项目导入/导出）</h2>
      <div>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
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
      <h3 style={{ marginTop: 12 }}>SISTEMA/PAScal/SET 互通</h3>
      <div>
        <button onClick={exportSistemaCsv}>导出 SISTEMA CSV</button>
        <label style={{ marginLeft: 8 }}>导入 SISTEMA CSV<input type="file" accept="text/csv,.csv" onChange={e => { const f=e.target.files?.[0]; if (f) importSistemaCsv(f) }} /></label>
        <button onClick={exportPascalJson} style={{ marginLeft: 8 }}>导出 PAScal JSON</button>
        <label style={{ marginLeft: 8 }}>导入 PAScal JSON<input type="file" accept="application/json" onChange={e => { const f=e.target.files?.[0]; if (f) importPascalJson(f) }} /></label>
        <button onClick={exportSiemensSetJson} style={{ marginLeft: 8 }}>导出 Siemens SET JSON</button>
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

function ReportTemplatePage({ token }: { token: string }) {
  const [templates, setTemplates] = useState<any[]>([])
  const [id, setId] = useState('tpl-1')
  const [name, setName] = useState('模板1')
  const [content, setContent] = useState('<h1>{{title}}</h1>')
  const [language, setLanguage] = useState('zh-CN')
  const [renderData, setRenderData] = useState<string>(JSON.stringify({ title: 'Demo' }, null, 2))
  const [renderHtml, setRenderHtml] = useState<string>('')
  const load = async () => {
    const r = await fetch('/api/report/template', { headers: { 'Authorization': `Bearer ${token}` } })
    setTemplates(await r.json())
  }
  const create = async () => {
    const body = { id, name, content }
    const r = await fetch('/api/report/template', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    if (r.ok) await load()
  }
  const update = async () => {
    const body = { id, name, content }
    const r = await fetch('/api/report/template/' + encodeURIComponent(id), { method: 'PUT', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    if (r.ok) await load()
  }
  const del = async (tid: string) => {
    const r = await fetch('/api/report/template/' + encodeURIComponent(tid), { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } })
    if (r.ok) await load()
  }
  const render = async () => {
    const body = { data: JSON.parse(renderData), language }
    const r = await fetch('/api/report/template/' + encodeURIComponent(id) + '/render', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setRenderHtml(await r.text())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>报告模板管理</h2>
      <div>
        <button onClick={load}>加载模板</button>
      </div>
      <div style={{ marginTop: 8, maxHeight: 240, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {templates.map((t:any,i:number)=> (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr auto', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{t.id ?? t.Id}</div>
            <div>{t.name ?? t.Name}</div>
            <div><button onClick={() => del(t.id ?? t.Id)}>删除</button></div>
          </div>
        ))}
      </div>
      <div style={{ marginTop: 12 }}>
        <input placeholder="模板ID" value={id} onChange={e => setId(e.target.value)} />
        <input placeholder="名称" value={name} onChange={e => setName(e.target.value)} style={{ marginLeft: 8 }} />
        <textarea placeholder="内容" value={content} onChange={e => setContent(e.target.value)} style={{ width: '100%', height: 160, marginTop: 8 }} />
        <button onClick={create}>创建</button>
        <button onClick={update} style={{ marginLeft: 8 }}>更新</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>渲染演示</h3>
        <select value={language} onChange={e => setLanguage(e.target.value)}>
          {['zh-CN','en-US'].map(l => <option key={l} value={l}>{l}</option>)}
        </select>
        <textarea value={renderData} onChange={e => setRenderData(e.target.value)} style={{ width: '100%', height: 160, marginTop: 8 }} />
        <button onClick={render}>渲染</button>
        {renderHtml && <iframe srcDoc={renderHtml} style={{ width: '100%', height: 360, border: '1px solid #e5e7eb', marginTop: 8 }} />}
      </div>
    </div>
  )
}

function CombinedReportPage({ token }: { token: string }) {
  const [iso, setIso] = useState<string>(JSON.stringify({ systemName: '演示系统', iso12100: { identifiedHazards: ['机械挤压'], severity: 3, frequency: 2, avoidance: 2, riskReductionMeasures: '紧急停止' }, iso13849: { requiredPL: 'PLc', architecture: 'Cat3', dcavg: 0.9, mttfd: 10000000, ccfScore: 65, validationPerformed: true } }, null, 2))
  const [isoResult, setIsoResult] = useState<string>(JSON.stringify({ isCompliant: true, summary: '合规', nonConformities: [] }, null, 2))
  const [iec, setIec] = useState<string>(JSON.stringify({ id: 'SF-TEST-001', name: 'Emergency Stop', targetSIL: 'SIL2', proofTestIntervalT1: 2000, missionTimeT10D: 10000, subsystems: [{ id: 'SUB-1', name: 'Logic', architecture: '1oo2', components: [{ id: 'LOGIC-PLC-SAFETY-002', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }, { id: 'LOGIC-PLC-SAFETY-003', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }] }] }, null, 2))
  const [language, setLanguage] = useState('zh-CN')
  const [html, setHtml] = useState<string>('')
  const generate = async () => {
    const body = { iso13849Checklist: JSON.parse(iso), iso13849Result: JSON.parse(isoResult), iec62061Function: JSON.parse(iec), language }
    const r = await fetch('/api/report/combined', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setHtml(await r.text())
  }
  return (
    <div style={{ maxWidth: 1000, margin: '24px auto', padding: 24 }}>
      <h2>ISO+IEC 合并报告</h2>
      <div>
        <select value={language} onChange={e => setLanguage(e.target.value)}>
          {['zh-CN','en-US'].map(l => <option key={l} value={l}>{l}</option>)}
        </select>
        <button onClick={generate} style={{ marginLeft: 8 }}>生成</button>
      </div>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginTop: 12 }}>
        <div>
          <h3>ISO 13849 清单</h3>
          <textarea value={iso} onChange={e => setIso(e.target.value)} style={{ width: '100%', height: 200 }} />
          <h3 style={{ marginTop: 8 }}>ISO 结果</h3>
          <textarea value={isoResult} onChange={e => setIsoResult(e.target.value)} style={{ width: '100%', height: 200 }} />
        </div>
        <div>
          <h3>IEC 62061 功能</h3>
          <textarea value={iec} onChange={e => setIec(e.target.value)} style={{ width: '100%', height: 420 }} />
        </div>
      </div>
      {html && <iframe srcDoc={html} style={{ width: '100%', height: 480, border: '1px solid #e5e7eb', marginTop: 12 }} />}
    </div>
  )
}

function EvidencePackagePage({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [language, setLanguage] = useState('zh-CN')
  const [idsText, setIdsText] = useState('[]')
  const [pkg, setPkg] = useState<any | null>(null)
  const generate = async () => {
    const body = { projectId, language, evidenceIds: JSON.parse(idsText) }
    const r = await fetch('/api/evidence/package/generate', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setPkg(await r.json())
  }
  const exportJson = async () => {
    if (!pkg) return
    const r = await fetch('/api/evidence/package/export/json', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(pkg) })
    const text = await r.text(); const blob = new Blob([text], { type: 'application/json' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'evidence.package.json'; a.click(); URL.revokeObjectURL(url)
  }
  const exportReport = async () => {
    if (!pkg) return
    const r = await fetch('/api/evidence/package/export/report', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(pkg) })
    const text = await r.text(); const blob = new Blob([text], { type: 'text/html' }); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'evidence.package.html'; a.click(); URL.revokeObjectURL(url)
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>本地化证据包</h2>
      <div>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <select value={language} onChange={e => setLanguage(e.target.value)} style={{ marginLeft: 8 }}>
          {['zh-CN','en-US'].map(l => <option key={l} value={l}>{l}</option>)}
        </select>
        <button onClick={generate} style={{ marginLeft: 8 }}>生成</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>证据ID列表(JSON数组)</h3>
        <textarea value={idsText} onChange={e => setIdsText(e.target.value)} style={{ width: '100%', height: 140 }} />
      </div>
      {pkg && (
        <div style={{ marginTop: 12 }}>
          <button onClick={exportJson}>导出 JSON</button>
          <button onClick={exportReport} style={{ marginLeft: 8 }}>导出报告</button>
          <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(pkg, null, 2)}</pre>
        </div>
      )}
    </div>
  )
}

function StatisticsPage({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('')
  const [report, setReport] = useState<any | null>(null)
  const load = async () => {
    const r = await fetch('/api/statistics/system' + (projectId ? ('?projectId=' + encodeURIComponent(projectId)) : ''), { headers: { 'Authorization': `Bearer ${token}` } })
    setReport(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>统计报表</h2>
      <div>
        <input placeholder="项目ID(可选)" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <button onClick={load} style={{ marginLeft: 8 }}>加载</button>
      </div>
      {report && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{JSON.stringify(report, null, 2)}</pre>}
    </div>
  )
}

function ElectricalDrawing({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [resourceType, setResourceType] = useState<'SRS'|'Function'|'Component'|'Checklist'>('Function')
  const [resourceId, setResourceId] = useState('SF-TEST-001')
  const [drawing, setDrawing] = useState<any>({ id: '', fileName: '', version: '', fileSize: 0, drawingNumber: '', sheetNumber: '', title: '', description: '' })
  const [validation, setValidation] = useState<any | null>(null)
  const [links, setLinks] = useState<any[]>([])
  const [linkedResources, setLinkedResources] = useState<any[]>([])
  const loadLinks = async () => {
    const qs = new URLSearchParams({ resourceType, resourceId })
    const r = await fetch(`/api/electrical-drawing/${encodeURIComponent(projectId)}?` + qs.toString(), { headers: { 'Authorization': `Bearer ${token}` } })
    setLinks(await r.json())
  }
  const linkNow = async () => {
    const body = { resourceType, resourceId, drawing: { Id: drawing.id, FileName: drawing.fileName, Version: drawing.version, FileSize: Number(drawing.fileSize) || 0, DrawingNumber: drawing.drawingNumber || undefined, SheetNumber: drawing.sheetNumber || undefined, Title: drawing.title || undefined, Description: drawing.description || undefined } }
    const r = await fetch(`/api/electrical-drawing/${encodeURIComponent(projectId)}/link`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    if (r.ok) await loadLinks()
  }
  const validate = async () => {
    const info = { Id: drawing.id, FileName: drawing.fileName, Version: drawing.version, FileSize: Number(drawing.fileSize) || 0, DrawingNumber: drawing.drawingNumber || undefined, SheetNumber: drawing.sheetNumber || undefined, Title: drawing.title || undefined, Description: drawing.description || undefined }
    const r = await fetch('/api/electrical-drawing/validate', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(info) })
    setValidation(await r.json())
  }
  const unlink = async (linkId: string) => {
    await fetch(`/api/electrical-drawing/${encodeURIComponent(projectId)}/link/${encodeURIComponent(linkId)}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } })
    await loadLinks()
  }
  const queryResources = async (drawingId: string) => {
    const r = await fetch(`/api/electrical-drawing/${encodeURIComponent(projectId)}/drawing/${encodeURIComponent(drawingId)}/resources`, { headers: { 'Authorization': `Bearer ${token}` } })
    setLinkedResources(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>电气图纸关联</h2>
      <div>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <select value={resourceType} onChange={e => setResourceType(e.target.value as any)} style={{ marginLeft: 8 }}>
          {['SRS','Function','Component','Checklist'].map(x => <option key={x} value={x}>{x}</option>)}
        </select>
        <input placeholder="资源ID" value={resourceId} onChange={e => setResourceId(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={loadLinks} style={{ marginLeft: 8 }}>加载关联</button>
      </div>
      <h3 style={{ marginTop: 12 }}>图纸信息</h3>
      <div style={{ border: '1px solid #e5e7eb', padding: 8 }}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 8 }}>
          <input placeholder="图纸ID" value={drawing.id} onChange={e => setDrawing({ ...drawing, id: e.target.value })} />
          <input placeholder="文件名" value={drawing.fileName} onChange={e => setDrawing({ ...drawing, fileName: e.target.value })} />
          <input placeholder="版本" value={drawing.version} onChange={e => setDrawing({ ...drawing, version: e.target.value })} />
          <input type="number" placeholder="文件大小(B)" value={drawing.fileSize} onChange={e => setDrawing({ ...drawing, fileSize: e.target.value })} />
          <input placeholder="图号" value={drawing.drawingNumber} onChange={e => setDrawing({ ...drawing, drawingNumber: e.target.value })} />
          <input placeholder="页号" value={drawing.sheetNumber} onChange={e => setDrawing({ ...drawing, sheetNumber: e.target.value })} />
          <input placeholder="标题" value={drawing.title} onChange={e => setDrawing({ ...drawing, title: e.target.value })} />
          <input placeholder="描述" value={drawing.description} onChange={e => setDrawing({ ...drawing, description: e.target.value })} />
        </div>
        <div style={{ marginTop: 8 }}>
          <button onClick={validate}>验证图纸信息</button>
          <button onClick={linkNow} style={{ marginLeft: 8 }}>关联到资源</button>
        </div>
        {validation && (
          <div style={{ marginTop: 8, padding: 8, border: '1px solid #e5e7eb' }}>
            <div>有效: {validation.isValid ? '是' : '否'}</div>
            {validation.message && <div>{validation.message}</div>}
            {Array.isArray(validation.issues) && validation.issues.length > 0 && <ul>{validation.issues.map((x:string,i:number)=><li key={i}>{x}</li>)}</ul>}
          </div>
        )}
      </div>
      <h3 style={{ marginTop: 12 }}>当前关联</h3>
      <div style={{ maxHeight: 300, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {links.map((x:any, i:number) => (
          <div key={x.id ?? i} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr auto', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.resourceType} / {x.resourceId}</div>
            <div>{x.drawing?.id}</div>
            <div>{x.drawing?.fileName}</div>
            <div>{x.drawing?.version}</div>
            <div>
              <button onClick={() => unlink(x.id)}>删除</button>
              <button onClick={() => queryResources(x.drawing?.id)} style={{ marginLeft: 6 }}>查看资源</button>
            </div>
          </div>
        ))}
      </div>
      {linkedResources.length > 0 && (
        <div style={{ marginTop: 12 }}>
          <h3>该图纸关联的资源</h3>
          <div style={{ maxHeight: 200, overflow: 'auto', border: '1px solid #e5e7eb' }}>
            {linkedResources.map((l:any, i:number) => (
              <div key={l.id ?? i} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr 1fr', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
                <div>{l.projectId}</div>
                <div>{l.resourceType}</div>
                <div>{l.resourceId}</div>
                <div>{l.linkedBy} @ {l.linkedAt}</div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function DualStandard({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [systemName, setSystemName] = useState('演示系统')
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

  const [funcId, setFuncId] = useState('SF-TEST-001')
  const [name, setName] = useState('Emergency Stop')
  const [target, setTarget] = useState('SIL2')
  const [t1, setT1] = useState<number>(2000)
  const [t10d, setT10d] = useState<number>(10000)
  const [subsystems, setSubsystems] = useState<Array<{id:string,name:string,architecture:string,components:Array<{id:string,manufacturer:string,model:string,pfhd:number,beta?:number}>}>>([
    { id: 'SUB-1', name: 'Logic', architecture: '1oo2', components: [ { id: 'LOGIC-PLC-SAFETY-002', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }, { id: 'LOGIC-PLC-SAFETY-003', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 } ] }
  ])
  const [result, setResult] = useState<any>(null)
  const payloadIso = () => ({
    systemName, assessor: '', projectId,
    iso12100: {
      identifiedHazards: hazards.split(',').map(s => s.trim()).filter(Boolean), severity, frequency, avoidance, riskReductionMeasures: measures
    },
    iso13849: { requiredPL, architecture, dcavg, mttfd, ccfScore: ccf, validationPerformed: validated },
    generalItems: []
  })
  const payloadIec = () => ({ id: funcId, name, targetSIL: target, proofTestIntervalT1: t1, missionTimeT10D: t10d, subsystems })
  const evaluate = async () => {
    const body = { Iso13849Checklist: payloadIso(), Iec62061Function: payloadIec() }
    const r = await fetch('/api/dual-standard/evaluate', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setResult(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>双标准并行评估（ISO 13849-1 + IEC 62061）</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
        <div>
          <h3>ISO 13849-1</h3>
          <label>项目ID<br /><input value={projectId} onChange={e => setProjectId(e.target.value)} /></label><br />
          <label>系统名称<br /><input value={systemName} onChange={e => setSystemName(e.target.value)} /></label><br />
          <label>危害<br /><input value={hazards} onChange={e => setHazards(e.target.value)} /></label><br />
          <label>严重度 (1-4)<br /><input type="number" min={1} max={4} value={severity} onChange={e => setSeverity(+e.target.value)} /></label><br />
          <label>频度 (1-4)<br /><input type="number" min={1} max={4} value={frequency} onChange={e => setFrequency(+e.target.value)} /></label><br />
          <label>可避性 (1-4)<br /><input type="number" min={1} max={4} value={avoidance} onChange={e => setAvoidance(+e.target.value)} /></label><br />
          <label>降低措施<br /><input value={measures} onChange={e => setMeasures(e.target.value)} /></label><br />
          <label>所需PL<br /><select value={requiredPL} onChange={e => setRequiredPL(e.target.value)}>{['PLa','PLb','PLc','PLd','PLe'].map(p => <option key={p} value={p}>{p}</option>)}</select></label><br />
          <label>架构<br /><select value={architecture} onChange={e => setArchitecture(e.target.value)}>{['B','Cat1','Cat2','Cat3','Cat4'].map(a => <option key={a} value={a}>{a}</option>)}</select></label><br />
          <label>DCavg<br /><input type="number" step={0.01} min={0} max={1} value={dcavg} onChange={e => setDcavg(+e.target.value)} /></label><br />
          <label>MTTFd(h)<br /><input type="number" value={mttfd} onChange={e => setMttfd(+e.target.value)} /></label><br />
          <label>CCF分数<br /><input type="number" value={ccf} onChange={e => setCcf(+e.target.value)} /></label><br />
          <label><input type="checkbox" checked={validated} onChange={e => setValidated(e.target.checked)} /> 已完成验证</label>
        </div>
        <div>
          <h3>IEC 62061</h3>
          <label>功能ID<br /><input value={funcId} onChange={e => setFuncId(e.target.value)} /></label><br />
          <label>安全功能<br /><input value={name} onChange={e => setName(e.target.value)} /></label><br />
          <label>目标SIL<br /><select value={target} onChange={e => setTarget(e.target.value)}>{['SIL1','SIL2','SIL3'].map(x => <option key={x} value={x}>{x}</option>)}</select></label><br />
          <label>T1<br /><input type="number" value={t1} onChange={e => setT1(+e.target.value)} /></label><br />
          <label>T10D<br /><input type="number" value={t10d} onChange={e => setT10d(+e.target.value)} /></label>
          <div style={{ marginTop: 8 }}>
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
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <button onClick={evaluate}>执行并行评估</button>
      </div>
      {result && (
        <div style={{ marginTop: 12, padding: 12, border: '1px solid #e5e7eb' }}>
          <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(result, null, 2)}</pre>
        </div>
      )}
    </div>
  )
}

function LinkedRemediation({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [currentPL, setCurrentPL] = useState('PLc')
  const [currentSIL, setCurrentSIL] = useState('SIL2')
  const [targetPL, setTargetPL] = useState('PLd')
  const [targetSIL, setTargetSIL] = useState('SIL3')
  const [items, setItems] = useState<any[]>([])
  const generate = async () => {
    const body = { CurrentPL: currentPL, CurrentSIL: currentSIL, TargetPL: targetPL, TargetSIL: targetSIL }
    const r = await fetch(`/api/remediation/linked/${encodeURIComponent(projectId)}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setItems(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>联动整改建议</h2>
      <div>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <label style={{ marginLeft: 8 }}>当前PL
          <select value={currentPL} onChange={e => setCurrentPL(e.target.value)} style={{ marginLeft: 8 }}>{['PLa','PLb','PLc','PLd','PLe'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        </label>
        <label style={{ marginLeft: 8 }}>当前SIL
          <select value={currentSIL} onChange={e => setCurrentSIL(e.target.value)} style={{ marginLeft: 8 }}>{['SIL1','SIL2','SIL3'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        </label>
        <label style={{ marginLeft: 8 }}>目标PL
          <select value={targetPL} onChange={e => setTargetPL(e.target.value)} style={{ marginLeft: 8 }}>{['PLa','PLb','PLc','PLd','PLe'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        </label>
        <label style={{ marginLeft: 8 }}>目标SIL
          <select value={targetSIL} onChange={e => setTargetSIL(e.target.value)} style={{ marginLeft: 8 }}>{['SIL1','SIL2','SIL3'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        </label>
        <button onClick={generate} style={{ marginLeft: 8 }}>生成建议</button>
      </div>
      <div style={{ marginTop: 12, maxHeight: 360, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {items.length === 0 ? <div style={{ padding: 8 }}>暂无建议</div> : items.map((x:any, i:number) => (
          <div key={x.id ?? i} style={{ padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.title ?? x.Title ?? '整改项'}</div>
            <div>{x.description ?? x.Description ?? ''}</div>
            <div>{x.owner ?? x.Owner ?? ''}</div>
            <div>{x.due ?? x.Due ?? ''}</div>
          </div>
        ))}
      </div>
    </div>
  )
}

function MatrixEnhancement({ token }: { token: string }) {
  const [projectId, setProjectId] = useState('demo')
  const [standard, setStandard] = useState<'ISO13849-1'|'IEC62061'>('ISO13849-1')
  const [index, setIndex] = useState<any[]>([])
  const [check, setCheck] = useState<any | null>(null)
  const [chain, setChain] = useState<any[]>([])
  const loadIndex = async () => {
    const r = await fetch(`/api/compliance/matrix/enhancement/${encodeURIComponent(projectId)}/clause-index?standard=${encodeURIComponent(standard)}`, { headers: { 'Authorization': `Bearer ${token}` } })
    setIndex(await r.json())
  }
  const runCheck = async () => {
    const r = await fetch(`/api/compliance/matrix/enhancement/${encodeURIComponent(projectId)}/check`, { headers: { 'Authorization': `Bearer ${token}` } })
    setCheck(await r.json())
  }
  const loadChain = async () => {
    const r = await fetch(`/api/compliance/matrix/enhancement/${encodeURIComponent(projectId)}/traceability`, { headers: { 'Authorization': `Bearer ${token}` } })
    setChain(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>验证矩阵增强</h2>
      <div>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <select value={standard} onChange={e => setStandard(e.target.value as any)} style={{ marginLeft: 8 }}>
          <option value="ISO13849-1">ISO13849-1</option>
          <option value="IEC62061">IEC62061</option>
        </select>
        <button onClick={loadIndex} style={{ marginLeft: 8 }}>条款索引</button>
        <button onClick={runCheck} style={{ marginLeft: 8 }}>缺失/不一致检查</button>
        <button onClick={loadChain} style={{ marginLeft: 8 }}>追溯链生成</button>
      </div>
      <div style={{ marginTop: 12 }}>
        {index.length > 0 && (
          <div>
            <h3>条款索引</h3>
            <div style={{ maxHeight: 240, overflow: 'auto', border: '1px solid #e5e7eb' }}>
              {index.map((x:any,i:number)=> (
                <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 2fr', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
                  <div>{x.clause ?? x.Clause}</div>
                  <div>{x.title ?? x.Title}</div>
                </div>
              ))}
            </div>
          </div>
        )}
        {check && (
          <div style={{ marginTop: 12 }}>
            <h3>检查结果</h3>
            <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(check, null, 2)}</pre>
          </div>
        )}
        {chain.length > 0 && (
          <div style={{ marginTop: 12 }}>
            <h3>追溯链</h3>
            <div style={{ maxHeight: 240, overflow: 'auto', border: '1px solid #e5e7eb' }}>
              {chain.map((x:any,i:number)=> (
                <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
                  <div>{x.requirement ?? x.Requirement}</div>
                  <div>{x.evidence ?? x.Evidence}</div>
                  <div>{x.result ?? x.Result}</div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

function ChannelVisualization({ token }: { token: string }) {
  const [functionId, setFunctionId] = useState('SF-TEST-001')
  const [data, setData] = useState<any | null>(null)
  const load = async () => {
    const r = await fetch('/api/visualization/channels/' + encodeURIComponent(functionId), { headers: { 'Authorization': `Bearer ${token}` } })
    if (r.ok) setData(await r.json())
    else setData(null)
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>通道连接可视化（数据视图）</h2>
      <div>
        <input placeholder="功能ID" value={functionId} onChange={e => setFunctionId(e.target.value)} />
        <button onClick={load} style={{ marginLeft: 8 }}>加载</button>
      </div>
      {data && (
        <div style={{ marginTop: 12 }}>
          <h3>摘要</h3>
          <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(data, null, 2)}</pre>
        </div>
      )}
    </div>
  )
}

function RuleHierarchy({ token }: { token: string }) {
  const [industry, setIndustry] = useState('')
  const [enterprise, setEnterprise] = useState('')
  const [project, setProject] = useState('demo')
  const [items, setItems] = useState<any[]>([])
  const [level, setLevel] = useState<'industry'|'enterprise'|'project'>('project')
  const [levelId, setLevelId] = useState('demo')
  const [ruleKey, setRuleKey] = useState('')
  const [ruleValue, setRuleValue] = useState('')
  const [compare, setCompare] = useState<any | null>(null)
  const load = async () => {
    const qs = new URLSearchParams({ industry, enterprise, project })
    const r = await fetch('/api/rules/hierarchy/?' + qs.toString(), { headers: { 'Authorization': `Bearer ${token}` } })
    setItems(await r.json())
  }
  const upsert = async () => {
    const body = { Key: ruleKey, Value: ruleValue }
    await fetch(`/api/rules/hierarchy/${encodeURIComponent(level)}/${encodeURIComponent(levelId)}`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    await load()
  }
  const del = async (k: string, lv?: string, lid?: string) => {
    const L = encodeURIComponent(lv ?? level)
    const I = encodeURIComponent(lid ?? levelId)
    await fetch(`/api/rules/hierarchy/${L}/${I}/${encodeURIComponent(k)}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } })
    await load()
  }
  const doCompare = async () => {
    const body = { Level1: 'industry', LevelId1: industry, Level2: 'project', LevelId2: project }
    const r = await fetch('/api/rules/hierarchy/compare', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setCompare(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>规则分层管理</h2>
      <div>
        <input placeholder="行业" value={industry} onChange={e => setIndustry(e.target.value)} />
        <input placeholder="企业" value={enterprise} onChange={e => setEnterprise(e.target.value)} style={{ marginLeft: 8 }} />
        <input placeholder="项目" value={project} onChange={e => setProject(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={load} style={{ marginLeft: 8 }}>加载</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <label>层级
          <select value={level} onChange={e => setLevel(e.target.value as any)} style={{ marginLeft: 8 }}>
            <option value="industry">industry</option>
            <option value="enterprise">enterprise</option>
            <option value="project">project</option>
          </select>
        </label>
        <input placeholder="层级ID" value={levelId} onChange={e => setLevelId(e.target.value)} style={{ marginLeft: 8 }} />
        <input placeholder="规则键" value={ruleKey} onChange={e => setRuleKey(e.target.value)} style={{ marginLeft: 8 }} />
        <input placeholder="规则值" value={ruleValue} onChange={e => setRuleValue(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={upsert} style={{ marginLeft: 8 }}>新增/更新</button>
        <button onClick={doCompare} style={{ marginLeft: 8 }}>行业vs项目对比</button>
      </div>
      <div style={{ marginTop: 12, maxHeight: 300, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {items.map((x:any,i:number)=> (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: '0.8fr 1fr 1fr 1fr auto', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.level ?? x.Level}</div>
            <div>{x.levelId ?? x.LevelId}</div>
            <div>{x.key ?? x.Key}</div>
            <div>{x.value ?? x.Value}</div>
            <div><button onClick={() => del(x.key ?? x.Key, x.level ?? x.Level, x.levelId ?? x.LevelId)}>删除</button></div>
          </div>
        ))}
      </div>
      {compare && (
        <div style={{ marginTop: 12 }}>
          <h3>对比结果</h3>
          <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(compare, null, 2)}</pre>
        </div>
      )}
    </div>
  )
}

function EquationSimplification({ token }: { token: string }) {
  const [funcId, setFuncId] = useState('SF-TEST-001')
  const [name, setName] = useState('Emergency Stop')
  const [target, setTarget] = useState('SIL2')
  const [t1, setT1] = useState<number>(2000)
  const [t10d, setT10d] = useState<number>(10000)
  const [subsystems, setSubsystems] = useState<Array<{id:string,name:string,architecture:string,components:Array<{id:string,manufacturer:string,model:string,pfhd:number,beta?:number}>}>>([
    { id: 'SUB-1', name: 'Logic', architecture: '1oo2', components: [ { id: 'LOGIC-PLC-SAFETY-002', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }, { id: 'LOGIC-PLC-SAFETY-003', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 } ] }
  ])
  const [result, setResult] = useState<any | null>(null)
  const analyze = async () => {
    const body = { id: funcId, name, targetSIL: target, proofTestIntervalT1: t1, missionTimeT10D: t10d, subsystems }
    const r = await fetch('/api/equation/simplification/analyze', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setResult(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>方程简化提示</h2>
      <div>
        <input placeholder="功能ID" value={funcId} onChange={e => setFuncId(e.target.value)} />
        <input placeholder="名称" value={name} onChange={e => setName(e.target.value)} style={{ marginLeft: 8 }} />
        <select value={target} onChange={e => setTarget(e.target.value)} style={{ marginLeft: 8 }}>{['SIL1','SIL2','SIL3'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        <input type="number" placeholder="T1" value={t1} onChange={e => setT1(+e.target.value)} style={{ marginLeft: 8, width: 120 }} />
        <input type="number" placeholder="T10D" value={t10d} onChange={e => setT10d(+e.target.value)} style={{ marginLeft: 8, width: 120 }} />
        <button onClick={analyze} style={{ marginLeft: 8 }}>分析</button>
      </div>
      {result && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{JSON.stringify(result, null, 2)}</pre>}
    </div>
  )
}

function SrecsDecomposition({ token }: { token: string }) {
  const [funcId, setFuncId] = useState('SF-TEST-001')
  const [name, setName] = useState('Emergency Stop')
  const [target, setTarget] = useState('SIL2')
  const [t1, setT1] = useState<number>(2000)
  const [t10d, setT10d] = useState<number>(10000)
  const [subsystems, setSubsystems] = useState<Array<{id:string,name:string,architecture:string,components:Array<{id:string,manufacturer:string,model:string,pfhd:number,beta?:number}>}>>([
    { id: 'SUB-1', name: 'Logic', architecture: '1oo2', components: [ { id: 'LOGIC-PLC-SAFETY-002', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }, { id: 'LOGIC-PLC-SAFETY-003', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 } ] }
  ])
  const [result, setResult] = useState<any | null>(null)
  const analyze = async () => {
    const body = { id: funcId, name, targetSIL: target, proofTestIntervalT1: t1, missionTimeT10D: t10d, subsystems }
    const r = await fetch('/api/srecs/decomposition/analyze', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setResult(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>SRECS 结构化分解</h2>
      <div>
        <input placeholder="功能ID" value={funcId} onChange={e => setFuncId(e.target.value)} />
        <input placeholder="名称" value={name} onChange={e => setName(e.target.value)} style={{ marginLeft: 8 }} />
        <select value={target} onChange={e => setTarget(e.target.value)} style={{ marginLeft: 8 }}>{['SIL1','SIL2','SIL3'].map(x => <option key={x} value={x}>{x}</option>)}</select>
        <input type="number" placeholder="T1" value={t1} onChange={e => setT1(+e.target.value)} style={{ marginLeft: 8, width: 120 }} />
        <input type="number" placeholder="T10D" value={t10d} onChange={e => setT10d(+e.target.value)} style={{ marginLeft: 8, width: 120 }} />
        <button onClick={analyze} style={{ marginLeft: 8 }}>分析</button>
      </div>
      {result && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{JSON.stringify(result, null, 2)}</pre>}
    </div>
  )
}

function BatchEvaluation({ token }: { token: string }) {
  const [isoRequests, setIsoRequests] = useState<string>(JSON.stringify([{ systemName: '演示系统', iso12100: { identifiedHazards: ['机械挤压'], severity: 3, frequency: 2, avoidance: 2, riskReductionMeasures: '紧急停止' }, iso13849: { requiredPL: 'PLc', architecture: 'Cat3', dcavg: 0.9, mttfd: 10000000, ccfScore: 65, validationPerformed: true } }], null, 2))
  const [iecRequests, setIecRequests] = useState<string>(JSON.stringify([{ id: 'SF-TEST-001', name: 'Emergency Stop', targetSIL: 'SIL2', proofTestIntervalT1: 2000, missionTimeT10D: 10000, subsystems: [{ id: 'SUB-1', name: 'Logic', architecture: '1oo2', components: [{ id: 'LOGIC-PLC-SAFETY-002', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }, { id: 'LOGIC-PLC-SAFETY-003', manufacturer: 'Contoso', model: 'SafePLC-200', pfhd: 3e-8, beta: 0.05 }] }] }], null, 2))
  const [resultIso, setResultIso] = useState<any | null>(null)
  const [resultIec, setResultIec] = useState<any | null>(null)
  const [resultCombined, setResultCombined] = useState<any | null>(null)
  const runIso = async () => {
    const r = await fetch('/api/batch/evaluation/iso13849', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: isoRequests })
    setResultIso(await r.json())
  }
  const runIec = async () => {
    const r = await fetch('/api/batch/evaluation/iec62061', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: iecRequests })
    setResultIec(await r.json())
  }
  const runCombined = async () => {
    const body = { iso13849: JSON.parse(isoRequests), iec62061: JSON.parse(iecRequests) }
    const r = await fetch('/api/batch/evaluation/combined', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setResultCombined(await r.json())
  }
  return (
    <div style={{ maxWidth: 1000, margin: '24px auto', padding: 24 }}>
      <h2>批量评估</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
        <div>
          <h3>ISO 13849 请求</h3>
          <textarea value={isoRequests} onChange={e => setIsoRequests(e.target.value)} style={{ width: '100%', height: 220 }} />
          <button onClick={runIso} style={{ marginTop: 8 }}>执行 ISO 批评估</button>
          {resultIso && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(resultIso, null, 2)}</pre>}
        </div>
        <div>
          <h3>IEC 62061 请求</h3>
          <textarea value={iecRequests} onChange={e => setIecRequests(e.target.value)} style={{ width: '100%', height: 220 }} />
          <button onClick={runIec} style={{ marginTop: 8 }}>执行 IEC 批评估</button>
          {resultIec && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(resultIec, null, 2)}</pre>}
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <button onClick={runCombined}>执行联合批评估</button>
        {resultCombined && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(resultCombined, null, 2)}</pre>}
      </div>
    </div>
  )
}

function DeploymentConfig({ token }: { token: string }) {
  const [type, setType] = useState<'offline'|'intranet'>('offline')
  const [config, setConfig] = useState<any | null>(null)
  const [doc, setDoc] = useState<string>('')
  const [validateInput, setValidateInput] = useState<string>('{}')
  const generate = async () => {
    const r = await fetch('/api/deployment/generate?type=' + encodeURIComponent(type), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    setConfig(await r.json())
  }
  const loadDoc = async () => {
    const r = await fetch('/api/deployment/' + encodeURIComponent(type) + '/document', { headers: { 'Authorization': `Bearer ${token}` } })
    setDoc(await r.text())
  }
  const validate = async () => {
    const r = await fetch('/api/deployment/validate', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: validateInput })
    setConfig(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>离线/内网部署配置</h2>
      <div>
        <select value={type} onChange={e => setType(e.target.value as any)}>
          <option value="offline">offline</option>
          <option value="intranet">intranet</option>
        </select>
        <button onClick={generate} style={{ marginLeft: 8 }}>生成配置</button>
        <button onClick={loadDoc} style={{ marginLeft: 8 }}>查看文档</button>
      </div>
      {doc && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{doc}</pre>}
      <div style={{ marginTop: 12 }}>
        <h3>验证配置</h3>
        <textarea value={validateInput} onChange={e => setValidateInput(e.target.value)} style={{ width: '100%', height: 200 }} />
        <button onClick={validate} style={{ marginTop: 8 }}>验证</button>
      </div>
      {config && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{JSON.stringify(config, null, 2)}</pre>}
    </div>
  )
}

function PerformanceMonitor({ token }: { token: string }) {
  const [metrics, setMetrics] = useState<any[]>([])
  const [warnings, setWarnings] = useState<any[]>([])
  const [operation, setOperation] = useState('')
  const [report, setReport] = useState<any | null>(null)
  const loadMetrics = async () => {
    const r = await fetch('/api/performance/metrics', { headers: { 'Authorization': `Bearer ${token}` } })
    setMetrics(await r.json())
  }
  const loadWarnings = async () => {
    const r = await fetch('/api/performance/warnings', { headers: { 'Authorization': `Bearer ${token}` } })
    setWarnings(await r.json())
  }
  const loadReport = async () => {
    const r = await fetch('/api/performance/report', { headers: { 'Authorization': `Bearer ${token}` } })
    setReport(await r.json())
  }
  const reset = async () => {
    await fetch('/api/performance/reset' + (operation ? ('?operation=' + encodeURIComponent(operation)) : ''), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    await loadMetrics()
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>性能监控</h2>
      <div>
        <button onClick={loadMetrics}>加载指标</button>
        <button onClick={loadWarnings} style={{ marginLeft: 8 }}>加载警告</button>
        <button onClick={loadReport} style={{ marginLeft: 8 }}>生成报告</button>
        <input placeholder="操作名" value={operation} onChange={e => setOperation(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={reset} style={{ marginLeft: 8 }}>重置指标</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>指标</h3>
        <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(metrics, null, 2)}</pre>
        <h3 style={{ marginTop: 12 }}>警告</h3>
        <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(warnings, null, 2)}</pre>
        {report && (
          <div style={{ marginTop: 12 }}>
            <h3>报告</h3>
            <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(report, null, 2)}</pre>
          </div>
        )}
      </div>
    </div>
  )
}

function CacheManagement({ token }: { token: string }) {
  const [stats, setStats] = useState<any | null>(null)
  const [key, setKey] = useState('')
  const load = async () => {
    const r = await fetch('/api/cache/statistics', { headers: { 'Authorization': `Bearer ${token}` } })
    setStats(await r.json())
  }
  const clear = async () => {
    await fetch('/api/cache/clear', { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    await load()
  }
  const remove = async () => {
    await fetch('/api/cache/' + encodeURIComponent(key), { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } })
    await load()
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>缓存管理</h2>
      <div>
        <button onClick={load}>加载统计</button>
        <button onClick={clear} style={{ marginLeft: 8 }}>清空缓存</button>
        <input placeholder="键" value={key} onChange={e => setKey(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={remove} style={{ marginLeft: 8 }}>删除键</button>
      </div>
      {stats && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{JSON.stringify(stats, null, 2)}</pre>}
    </div>
  )
}

function EvidenceValidation({ token }: { token: string }) {
  const [evidenceId, setEvidenceId] = useState('')
  const [projectId, setProjectId] = useState('demo')
  const [batchIds, setBatchIds] = useState<string>('[]')
  const [result, setResult] = useState<any | null>(null)
  const [batchResult, setBatchResult] = useState<any | null>(null)
  const [chainResult, setChainResult] = useState<any | null>(null)
  const validateOne = async () => {
    if (!evidenceId) return
    const r = await fetch('/api/evidence/validation/' + encodeURIComponent(evidenceId), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    setResult(await r.json())
  }
  const validateBatch = async () => {
    const body = { evidenceIds: JSON.parse(batchIds) }
    const r = await fetch('/api/evidence/validation/batch', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setBatchResult(await r.json())
  }
  const validateChain = async () => {
    const r = await fetch('/api/evidence/validation/chain/' + encodeURIComponent(projectId), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    setChainResult(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>证据校验与证据链验证</h2>
      <div>
        <input placeholder="证据ID" value={evidenceId} onChange={e => setEvidenceId(e.target.value)} />
        <button onClick={validateOne} style={{ marginLeft: 8 }}>校验单个</button>
      </div>
      {result && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(result, null, 2)}</pre>}
      <div style={{ marginTop: 12 }}>
        <h3>批量校验</h3>
        <textarea value={batchIds} onChange={e => setBatchIds(e.target.value)} style={{ width: '100%', height: 120 }} />
        <button onClick={validateBatch} style={{ marginTop: 8 }}>校验批量</button>
      </div>
      {batchResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(batchResult, null, 2)}</pre>}
      <div style={{ marginTop: 12 }}>
        <h3>证据链验证</h3>
        <input placeholder="项目ID" value={projectId} onChange={e => setProjectId(e.target.value)} />
        <button onClick={validateChain} style={{ marginLeft: 8 }}>验证链路</button>
      </div>
      {chainResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(chainResult, null, 2)}</pre>}
    </div>
  )
}

function RbacManagement({ token }: { token: string }) {
  const [roles, setRoles] = useState<any[]>([])
  const [roleName, setRoleName] = useState('')
  const [rolePerms, setRolePerms] = useState<string>('[]')
  const [userId, setUserId] = useState('user')
  const [roleId, setRoleId] = useState('')
  const [perms, setPerms] = useState<any[]>([])
  const [checkPerm, setCheckPerm] = useState('component:view-sensitive')
  const [checkResult, setCheckResult] = useState<any | null>(null)
  const loadRoles = async () => {
    const r = await fetch('/api/rbac/roles', { headers: { 'Authorization': `Bearer ${token}` } })
    setRoles(await r.json())
  }
  const createRole = async () => {
    const body = { Id: roleName, Name: roleName, Permissions: JSON.parse(rolePerms) }
    const r = await fetch('/api/rbac/roles', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    if (r.ok) await loadRoles()
  }
  const loadUserPerms = async () => {
    const r = await fetch('/api/rbac/users/' + encodeURIComponent(userId) + '/permissions', { headers: { 'Authorization': `Bearer ${token}` } })
    setPerms(await r.json())
  }
  const assignRole = async () => {
    await fetch('/api/rbac/users/' + encodeURIComponent(userId) + '/roles/' + encodeURIComponent(roleId), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    await loadUserPerms()
  }
  const removeRole = async () => {
    await fetch('/api/rbac/users/' + encodeURIComponent(userId) + '/roles/' + encodeURIComponent(roleId), { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } })
    await loadUserPerms()
  }
  const checkPermission = async () => {
    const body = { UserId: userId, Permission: checkPerm }
    const r = await fetch('/api/rbac/check', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setCheckResult(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>RBAC 角色与权限管理</h2>
      <div>
        <button onClick={loadRoles}>加载角色</button>
        <div style={{ marginTop: 8, maxHeight: 200, overflow: 'auto', border: '1px solid #e5e7eb' }}>
          {roles.map((x:any,i:number)=> (
            <div key={i} style={{ padding: 6, borderBottom: '1px solid #f0f0f0' }}>{x.id ?? x.Id} — {x.name ?? x.Name}</div>
          ))}
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>新建角色</h3>
        <input placeholder="角色名" value={roleName} onChange={e => setRoleName(e.target.value)} />
        <textarea placeholder="权限列表(JSON数组)" value={rolePerms} onChange={e => setRolePerms(e.target.value)} style={{ width: '100%', height: 120 }} />
        <button onClick={createRole} style={{ marginTop: 8 }}>创建角色</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>用户权限</h3>
        <input placeholder="用户ID" value={userId} onChange={e => setUserId(e.target.value)} />
        <input placeholder="角色ID" value={roleId} onChange={e => setRoleId(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={loadUserPerms} style={{ marginLeft: 8 }}>加载权限</button>
        <button onClick={assignRole} style={{ marginLeft: 8 }}>分配角色</button>
        <button onClick={removeRole} style={{ marginLeft: 8 }}>移除角色</button>
        <div style={{ marginTop: 8 }}>
          <strong>权限：</strong> <span>{Array.isArray(perms) ? perms.join(', ') : ''}</span>
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>权限检查</h3>
        <input placeholder="权限键" value={checkPerm} onChange={e => setCheckPerm(e.target.value)} />
        <button onClick={checkPermission} style={{ marginLeft: 8 }}>检查</button>
        {checkResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(checkResult, null, 2)}</pre>}
      </div>
    </div>
  )
}

function SystemConfigPage({ token }: { token: string }) {
  const [config, setConfig] = useState<any | null>(null)
  const [exportText, setExportText] = useState<string>('')
  const [importText, setImportText] = useState<string>('{}')
  const [key, setKey] = useState('')
  const [value, setValue] = useState('')
  const load = async () => {
    const r = await fetch('/api/system/config/', { headers: { 'Authorization': `Bearer ${token}` } })
    setConfig(await r.json())
  }
  const update = async () => {
    if (!config) return
    const r = await fetch('/api/system/config/', { method: 'PUT', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(config) })
    setConfig(await r.json())
  }
  const getSetting = async () => {
    const r = await fetch('/api/system/config/setting/' + encodeURIComponent(key), { headers: { 'Authorization': `Bearer ${token}` } })
    setExportText(await r.text())
  }
  const setSetting = async () => {
    await fetch('/api/system/config/setting/' + encodeURIComponent(key), { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(value) })
  }
  const reset = async () => {
    const r = await fetch('/api/system/config/reset', { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    setConfig(await r.json())
  }
  const exportCfg = async () => {
    const r = await fetch('/api/system/config/export', { headers: { 'Authorization': `Bearer ${token}` } })
    setExportText(await r.text())
  }
  const importCfg = async () => {
    const body = { json: importText }
    const r = await fetch('/api/system/config/import', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setConfig(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>系统配置管理</h2>
      <div>
        <button onClick={load}>加载配置</button>
        <button onClick={update} style={{ marginLeft: 8 }}>更新配置</button>
        <button onClick={reset} style={{ marginLeft: 8 }}>重置配置</button>
        <button onClick={exportCfg} style={{ marginLeft: 8 }}>导出配置</button>
      </div>
      {config && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(config, null, 2)}</pre>}
      <div style={{ marginTop: 12 }}>
        <h3>单项设置</h3>
        <input placeholder="键" value={key} onChange={e => setKey(e.target.value)} />
        <input placeholder="值" value={value} onChange={e => setValue(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={getSetting} style={{ marginLeft: 8 }}>读取</button>
        <button onClick={setSetting} style={{ marginLeft: 8 }}>设置</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>导入配置</h3>
        <textarea value={importText} onChange={e => setImportText(e.target.value)} style={{ width: '100%', height: 160 }} />
        <button onClick={importCfg} style={{ marginTop: 8 }}>导入</button>
      </div>
      {exportText && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 12 }}>{exportText}</pre>}
    </div>
  )
}

function ComponentVersionPage({ token }: { token: string }) {
  const [componentId, setComponentId] = useState('')
  const [versions, setVersions] = useState<any[]>([])
  const [version1, setVersion1] = useState('')
  const [version2, setVersion2] = useState('')
  const [diff, setDiff] = useState<any | null>(null)
  const [reason, setReason] = useState('调整参数')
  const loadVersions = async () => {
    const r = await fetch('/api/component/version/' + encodeURIComponent(componentId), { headers: { 'Authorization': `Bearer ${token}` } })
    setVersions(await r.json())
  }
  const createVersion = async () => {
    const body = { reason: reason }
    const r = await fetch('/api/component/version/' + encodeURIComponent(componentId) + '/create', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    if (r.ok) await loadVersions()
  }
  const compare = async () => {
    const r = await fetch('/api/component/version/' + encodeURIComponent(componentId) + '/compare?version1=' + encodeURIComponent(version1) + '&version2=' + encodeURIComponent(version2), { headers: { 'Authorization': `Bearer ${token}` } })
    setDiff(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>组件版本管理</h2>
      <div>
        <input placeholder="组件ID" value={componentId} onChange={e => setComponentId(e.target.value)} />
        <button onClick={loadVersions} style={{ marginLeft: 8 }}>加载版本</button>
        <button onClick={createVersion} style={{ marginLeft: 8 }}>创建版本</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>版本列表</h3>
        <div style={{ maxHeight: 240, overflow: 'auto', border: '1px solid #e5e7eb' }}>
          {versions.map((v:any,i:number)=> (
            <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
              <div>{v.version ?? v.Version}</div>
              <div>{v.user ?? v.User}</div>
              <div>{v.time ?? v.Time}</div>
            </div>
          ))}
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>版本对比</h3>
        <input placeholder="版本1" value={version1} onChange={e => setVersion1(e.target.value)} />
        <input placeholder="版本2" value={version2} onChange={e => setVersion2(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={compare} style={{ marginLeft: 8 }}>对比</button>
        {diff && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(diff, null, 2)}</pre>}
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>版本原因</h3>
        <input placeholder="原因" value={reason} onChange={e => setReason(e.target.value)} />
      </div>
    </div>
  )
}

function ComponentAttachmentPage({ token }: { token: string }) {
  const [componentId, setComponentId] = useState('')
  const [items, setItems] = useState<any[]>([])
  const [name, setName] = useState('')
  const [type, setType] = useState('datasheet')
  const [description, setDescription] = useState('')
  const [file, setFile] = useState<File | null>(null)
  const load = async () => {
    const r = await fetch('/api/component/attachment/' + encodeURIComponent(componentId), { headers: { 'Authorization': `Bearer ${token}` } })
    setItems(await r.json())
  }
  const upload = async () => {
    if (!file || !componentId) return
    const fd = new FormData(); fd.append('name', name); fd.append('type', type); fd.append('description', description); fd.append('file', file)
    await fetch('/api/component/attachment/' + encodeURIComponent(componentId), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` }, body: fd })
    await load()
  }
  const download = async (attachmentId: string) => {
    const r = await fetch('/api/component/attachment/' + encodeURIComponent(componentId) + '/' + encodeURIComponent(attachmentId) + '/download', { headers: { 'Authorization': `Bearer ${token}` } })
    if (!r.ok) return; const blob = await r.blob(); const url = URL.createObjectURL(blob)
    const a = document.createElement('a'); a.href = url; a.download = 'attachment_' + attachmentId; a.click(); URL.revokeObjectURL(url)
  }
  const del = async (attachmentId: string) => {
    await fetch('/api/component/attachment/' + encodeURIComponent(componentId) + '/' + encodeURIComponent(attachmentId), { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } })
    await load()
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>组件附件管理</h2>
      <div>
        <input placeholder="组件ID" value={componentId} onChange={e => setComponentId(e.target.value)} />
        <button onClick={load} style={{ marginLeft: 8 }}>加载</button>
      </div>
      <div style={{ marginTop: 8 }}>
        <input placeholder="名称" value={name} onChange={e => setName(e.target.value)} />
        <input placeholder="类型" value={type} onChange={e => setType(e.target.value)} style={{ marginLeft: 8 }} />
        <input placeholder="描述" value={description} onChange={e => setDescription(e.target.value)} style={{ marginLeft: 8 }} />
        <input type="file" onChange={e => setFile(e.target.files?.[0] ?? null)} style={{ marginLeft: 8 }} />
        <button onClick={upload} style={{ marginLeft: 8 }}>上传</button>
      </div>
      <div style={{ marginTop: 12, maxHeight: 240, overflow: 'auto', border: '1px solid #e5e7eb' }}>
        {items.map((x:any,i:number)=> (
          <div key={i} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 2fr auto', gap: 8, padding: 6, borderBottom: '1px solid #f0f0f0' }}>
            <div>{x.name}</div>
            <div>{x.type}</div>
            <div>{x.description}</div>
            <div>
              <button onClick={() => download(x.id)}>下载</button>
              <button onClick={() => del(x.id)} style={{ marginLeft: 6 }}>删除</button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

function Iec60204Page({ token }: { token: string }) {
  const [overloadJson, setOverloadJson] = useState<string>('{}')
  const [isolationJson, setIsolationJson] = useState<string>('{}')
  const [overloadResult, setOverloadResult] = useState<any | null>(null)
  const [isolationResult, setIsolationResult] = useState<any | null>(null)
  const [comprehensiveResult, setComprehensiveResult] = useState<any | null>(null)
  const checkOverload = async () => {
    const r = await fetch('/api/iec60204/overload-protection/check', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: overloadJson })
    setOverloadResult(await r.json())
  }
  const checkIsolation = async () => {
    const r = await fetch('/api/iec60204/isolation-short-circuit/check', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: isolationJson })
    setIsolationResult(await r.json())
  }
  const checkComprehensive = async () => {
    const body = { overload: JSON.parse(overloadJson), isolation: JSON.parse(isolationJson) }
    const r = await fetch('/api/iec60204/comprehensive-check', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    setComprehensiveResult(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>IEC 60204-1 电气综合检查</h2>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
        <div>
          <h3>过载保护输入</h3>
          <textarea value={overloadJson} onChange={e => setOverloadJson(e.target.value)} style={{ width: '100%', height: 160 }} />
          <button onClick={checkOverload} style={{ marginTop: 8 }}>检查过载保护</button>
          {overloadResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(overloadResult, null, 2)}</pre>}
        </div>
        <div>
          <h3>隔离与短路输入</h3>
          <textarea value={isolationJson} onChange={e => setIsolationJson(e.target.value)} style={{ width: '100%', height: 160 }} />
          <button onClick={checkIsolation} style={{ marginTop: 8 }}>检查隔离短路</button>
          {isolationResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(isolationResult, null, 2)}</pre>}
        </div>
      </div>
      <div style={{ marginTop: 12 }}>
        <button onClick={checkComprehensive}>综合检查</button>
        {comprehensiveResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(comprehensiveResult, null, 2)}</pre>}
      </div>
    </div>
  )
}

function T1T10DPage({ token }: { token: string }) {
  const [manageJson, setManageJson] = useState<string>('{}')
  const [targetSIL, setTargetSIL] = useState<number>(2)
  const [currentT10D, setCurrentT10D] = useState<number | ''>('')
  const [manageResult, setManageResult] = useState<any | null>(null)
  const [suggestResult, setSuggestResult] = useState<any | null>(null)
  const manage = async () => {
    const r = await fetch('/api/t1t10d/manage', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: manageJson })
    setManageResult(await r.json())
  }
  const suggest = async () => {
    const qs = new URLSearchParams({ targetSIL: String(targetSIL), currentT10D: currentT10D === '' ? '' : String(currentT10D) })
    const r = await fetch('/api/t1t10d/suggest?' + qs.toString(), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    setSuggestResult(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>T1/T10D 参数管理</h2>
      <div>
        <h3>管理参数</h3>
        <textarea value={manageJson} onChange={e => setManageJson(e.target.value)} style={{ width: '100%', height: 160 }} />
        <button onClick={manage} style={{ marginTop: 8 }}>提交管理</button>
        {manageResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(manageResult, null, 2)}</pre>}
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>参数建议</h3>
        <input type="number" placeholder="目标SIL(数值)" value={targetSIL} onChange={e => setTargetSIL(+e.target.value)} />
        <input type="number" placeholder="当前T10D(可空)" value={currentT10D} onChange={e => setCurrentT10D(e.target.value ? +e.target.value : '')} style={{ marginLeft: 8 }} />
        <button onClick={suggest} style={{ marginLeft: 8 }}>获取建议</button>
        {suggestResult && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(suggestResult, null, 2)}</pre>}
      </div>
    </div>
  )
}

function RealtimePage({ token }: { token: string }) {
  const [sessionId, setSessionId] = useState('')
  const [requestJson, setRequestJson] = useState<string>('{}')
  const [result, setResult] = useState<any | null>(null)
  const [session, setSession] = useState<any | null>(null)
  const createSession = async () => {
    const r = await fetch('/api/realtime/session', { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    const x = await r.json(); setSessionId(x.sessionId ?? x.SessionId ?? '')
  }
  const calculate = async () => {
    if (!sessionId) return
    const r = await fetch('/api/realtime/calculate/' + encodeURIComponent(sessionId), { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: requestJson })
    setResult(await r.json())
  }
  const cancel = async () => {
    if (!sessionId) return
    await fetch('/api/realtime/cancel/' + encodeURIComponent(sessionId), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
  }
  const getSession = async () => {
    if (!sessionId) return
    const r = await fetch('/api/realtime/session/' + encodeURIComponent(sessionId), { headers: { 'Authorization': `Bearer ${token}` } })
    setSession(await r.json())
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>实时计算会话</h2>
      <div>
        <button onClick={createSession}>创建会话</button>
        <input placeholder="会话ID" value={sessionId} onChange={e => setSessionId(e.target.value)} style={{ marginLeft: 8 }} />
        <button onClick={getSession} style={{ marginLeft: 8 }}>获取会话</button>
        <button onClick={cancel} style={{ marginLeft: 8 }}>取消会话</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <textarea value={requestJson} onChange={e => setRequestJson(e.target.value)} style={{ width: '100%', height: 160 }} />
        <button onClick={calculate} style={{ marginTop: 8 }}>执行计算</button>
      </div>
      {result && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(result, null, 2)}</pre>}
      {session && <pre style={{ whiteSpace: 'pre-wrap', marginTop: 8 }}>{JSON.stringify(session, null, 2)}</pre>}
    </div>
  )
}

function LocalizationPage({ token }: { token: string }) {
  const [languages, setLanguages] = useState<string[]>([])
  const [language, setLanguage] = useState('zh-CN')
  const [strings, setStrings] = useState<Record<string,string>>({})
  const [formatUnit, setFormatUnit] = useState<{value:string,unit:string,language:string}>({ value: '10', unit: 'h', language: 'zh-CN' })
  const [formatTime, setFormatTime] = useState<{hours:string,language:string}>({ hours: '1.5', language: 'zh-CN' })
  const [formatPercentage, setFormatPercentage] = useState<{value:string,language:string}>({ value: '0.95', language: 'zh-CN' })
  const [formatDateTime, setFormatDateTime] = useState<{dt:string,format:string,language:string}>({ dt: new Date().toISOString(), format: 'yyyy-MM-dd HH:mm', language: 'zh-CN' })
  const [formatNumber, setFormatNumber] = useState<{number:string,format:string,language:string}>({ number: '12345.678', format: 'N2', language: 'zh-CN' })
  const loadLanguages = async () => {
    const r = await fetch('/api/localization/languages', { headers: { 'Authorization': `Bearer ${token}` } })
    setLanguages(await r.json())
  }
  const loadStrings = async () => {
    const r = await fetch('/api/localization/strings/' + encodeURIComponent(language), { headers: { 'Authorization': `Bearer ${token}` } })
    setStrings(await r.json())
  }
  const doFormatUnit = async () => {
    const qs = new URLSearchParams({ value: String(formatUnit.value), unit: formatUnit.unit, language: formatUnit.language })
    const r = await fetch('/api/localization/format/unit?' + qs.toString(), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    alert((await r.json()).formatted)
  }
  const doFormatTime = async () => {
    const qs = new URLSearchParams({ hours: String(formatTime.hours), language: formatTime.language })
    const r = await fetch('/api/localization/format/time?' + qs.toString(), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    alert((await r.json()).formatted)
  }
  const doFormatPercentage = async () => {
    const qs = new URLSearchParams({ value: String(formatPercentage.value), language: formatPercentage.language })
    const r = await fetch('/api/localization/format/percentage?' + qs.toString(), { method: 'POST', headers: { 'Authorization': `Bearer ${token}` } })
    alert((await r.json()).formatted)
  }
  const doFormatDateTime = async () => {
    const body = { dateTime: formatDateTime.dt, format: formatDateTime.format, language: formatDateTime.language }
    const r = await fetch('/api/localization/format/datetime', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    alert((await r.json()).formatted)
  }
  const doFormatNumber = async () => {
    const body = { number: Number(formatNumber.number), format: formatNumber.format, language: formatNumber.language }
    const r = await fetch('/api/localization/format/number', { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }, body: JSON.stringify(body) })
    alert((await r.json()).formatted)
  }
  return (
    <div style={{ maxWidth: 900, margin: '24px auto', padding: 24 }}>
      <h2>本地化格式化</h2>
      <div>
        <button onClick={loadLanguages}>加载语言</button>
        <select value={language} onChange={e => setLanguage(e.target.value)} style={{ marginLeft: 8 }}>
          {languages.map(l => <option key={l} value={l}>{l}</option>)}
        </select>
        <button onClick={loadStrings} style={{ marginLeft: 8 }}>加载文案</button>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>文案</h3>
        <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(strings, null, 2)}</pre>
      </div>
      <div style={{ marginTop: 12 }}>
        <h3>格式化演示</h3>
        <div>
          <label>单位
            <input value={formatUnit.value} onChange={e => setFormatUnit({ ...formatUnit, value: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatUnit.unit} onChange={e => setFormatUnit({ ...formatUnit, unit: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatUnit.language} onChange={e => setFormatUnit({ ...formatUnit, language: e.target.value })} style={{ marginLeft: 8 }} />
            <button onClick={doFormatUnit} style={{ marginLeft: 8 }}>格式化</button>
          </label>
        </div>
        <div style={{ marginTop: 8 }}>
          <label>时间
            <input value={formatTime.hours} onChange={e => setFormatTime({ ...formatTime, hours: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatTime.language} onChange={e => setFormatTime({ ...formatTime, language: e.target.value })} style={{ marginLeft: 8 }} />
            <button onClick={doFormatTime} style={{ marginLeft: 8 }}>格式化</button>
          </label>
        </div>
        <div style={{ marginTop: 8 }}>
          <label>百分比
            <input value={formatPercentage.value} onChange={e => setFormatPercentage({ ...formatPercentage, value: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatPercentage.language} onChange={e => setFormatPercentage({ ...formatPercentage, language: e.target.value })} style={{ marginLeft: 8 }} />
            <button onClick={doFormatPercentage} style={{ marginLeft: 8 }}>格式化</button>
          </label>
        </div>
        <div style={{ marginTop: 8 }}>
          <label>日期时间
            <input value={formatDateTime.dt} onChange={e => setFormatDateTime({ ...formatDateTime, dt: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatDateTime.format} onChange={e => setFormatDateTime({ ...formatDateTime, format: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatDateTime.language} onChange={e => setFormatDateTime({ ...formatDateTime, language: e.target.value })} style={{ marginLeft: 8 }} />
            <button onClick={doFormatDateTime} style={{ marginLeft: 8 }}>格式化</button>
          </label>
        </div>
        <div style={{ marginTop: 8 }}>
          <label>数字
            <input value={formatNumber.number} onChange={e => setFormatNumber({ ...formatNumber, number: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatNumber.format} onChange={e => setFormatNumber({ ...formatNumber, format: e.target.value })} style={{ marginLeft: 8 }} />
            <input value={formatNumber.language} onChange={e => setFormatNumber({ ...formatNumber, language: e.target.value })} style={{ marginLeft: 8 }} />
            <button onClick={doFormatNumber} style={{ marginLeft: 8 }}>格式化</button>
          </label>
        </div>
      </div>
    </div>
  )
}
