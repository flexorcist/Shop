import { useState, useEffect } from 'react';

const ORDERS_API  = 'http://localhost:5001/api';
const PAY_API     = 'http://localhost:5002/api';

/* генератор GUID */
function newGuid() {
  return crypto.randomUUID();
}

/* обновление списка заказов */
async function api(url, opt) {
  const r = await fetch(url, opt);
  if (!r.ok) throw new Error(`${r.status} ${r.statusText}`);
  return r.status === 204 ? null : r.json();
}

export default function App() {
  /* создаём пользователю GUID один раз за сессию */
  const [userId]   = useState(() => localStorage.userId ?? (localStorage.userId = newGuid()));
  const [balance, setBalance] = useState(null);
  const [orders,  setOrders]  = useState([]);
  const [amount,  setAmount]  = useState(0);
  const [desc,    setDesc]    = useState('');
  const [auto,    setAuto]    = useState(true);
  const [msg,     setMsg]     = useState('');

  const show = m => { setMsg(m); setTimeout(() => setMsg(''), 2500); };

  const ensureAccount = async () => {
    try { await api(`${PAY_API}/accounts?userId=${userId}`, { method:'POST' }); }
    catch { }
  };

  const loadBalance = async () => {
    try {
      const { balance } = await api(`${PAY_API}/accounts/${userId}`);
      setBalance(balance);
    } catch { setBalance(null); }
  };

  const loadOrders = async () => {
    try { setOrders(await api(`${ORDERS_API}/orders`)); } catch {}
  };

  useEffect(() => {
    (async () => { await ensureAccount(); await loadBalance(); await loadOrders(); })();
  }, [userId]);

  useEffect(() => {
    if (!auto) return;
    const id = setInterval(() => { loadBalance(); loadOrders(); }, 1000);
    return () => clearInterval(id);
  }, [auto]);

  return (
    <div style={{fontFamily:'system-ui',padding:'1.5rem',maxWidth:720,margin:'0 auto'}}>
      <h1>Summer Sale Shop</h1>

      <p><strong>User&nbsp;Id:</strong> {userId}</p>
      <p><strong>Balance:</strong> {balance ?? '–'}</p>

      <section style={{marginBottom:20}}>
        <h2>Пополнение счета</h2>
        <input type="number" min="0" value={amount} onChange={e=>setAmount(+e.target.value)} style={{width:100}}/>
        &nbsp;
        <button disabled={amount<=0}
                onClick={async()=>{await api(`${PAY_API}/accounts/${userId}/top-up?amount=${amount}`,{method:'POST'}); loadBalance(); show('Top-up OK');}}>
          Top-up
        </button>
      </section>

      <section style={{marginBottom:20}}>
        <h2>Создать заказ</h2>
        <input type="number" min="1" value={amount} onChange={e=>setAmount(+e.target.value)} style={{width:100}}/>
        &nbsp;<input placeholder="Description" value={desc} onChange={e=>setDesc(e.target.value)} style={{width:240}}/>
        &nbsp;
        <button disabled={amount<=0}
                onClick={async()=>{
                  await api(`${ORDERS_API}/orders`,{
                    method:'POST',
                    headers:{'Content-Type':'application/json'},
                    body:JSON.stringify({userId,amount,description:desc})
                  });
                  show('Order created');
                  loadOrders();
                }}>
          Create
        </button>
      </section>

      <section>
        <h2>
          Orders&nbsp;
          <label style={{fontSize:12}}>
            <input type="checkbox" checked={auto} onChange={e=>setAuto(e.target.checked)}/> auto 1 s
          </label>
          &nbsp;
          <button onClick={loadOrders}>Refresh</button>
        </h2>

        <table border="1" cellPadding="4" style={{borderCollapse:'collapse',width:'100%'}}>
          <thead style={{background:'#eee'}}>
            <tr><th>Id</th><th>Amount</th><th>Description</th><th>Status</th></tr>
          </thead>
          <tbody>
            {orders.map(o=>(
              <tr key={o.id}>
                <td style={{fontSize:'0.7rem'}}>{o.id}</td>
                <td>{o.amount}</td>
                <td>{o.description}</td>
                <td>{o.status}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      {msg && <div style={{
        position:'fixed',bottom:20,right:20,background:'#333',color:'#fff',
        padding:'8px 14px',borderRadius:6,opacity:0.9}}>{msg}</div>}
    </div>
  );
}
