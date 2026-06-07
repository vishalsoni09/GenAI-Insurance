import React, { useState } from 'react';
import api from '../services/api';

export default function LoanEligibility(){
  const [name, setName] = useState('');
  const [income, setIncome] = useState('');
  const [creditScore, setCreditScore] = useState('');
  const [amount, setAmount] = useState('');
  const [term, setTerm] = useState('');
  const [result, setResult] = useState(null);


  const submit = async () => {
    const req = {
      customer: { name, monthlyIncome: parseFloat(income || 0), creditScore: parseInt(creditScore || '0') },
      amount: parseFloat(amount || 0),
      termMonths: parseInt(term || '0')
    };
    try {
      const resp = await api.post('/api/loan/assess', req);
      console.debug('loan assess response', resp);
      const d = resp.data || {};
      // If server returned HTML (SPA index) instead of JSON it's usually a proxy/baseURL issue
      const contentType = resp.headers && (resp.headers['content-type'] || resp.headers['Content-Type']);
      if (typeof resp.data === 'string' && (contentType && contentType.includes('text/html'))) {
        setResult({ eligible: false, monthlyPayment: 0, reason: 'Server returned HTML — check API base URL / proxy configuration.' });
        return;
      }
      // normalize server response (support PascalCase and camelCase)
      const normalized = {
        eligible: d.eligible ?? d.Eligible ?? false,
        monthlyPayment: d.monthlyPayment ?? d.MonthlyPayment ?? 0,
        interestRatePercent: d.interestRatePercent ?? d.InterestRatePercent ?? 0,
        reason: d.reason ?? d.Reason ?? ''
      };
      setResult(normalized);
    }
    catch (err) {
      console.error('Loan assess error', err);
      const serverMsg = err?.response?.data ?? err?.message ?? 'Error contacting server';
      setResult({ eligible: false, reason: typeof serverMsg === 'string' ? serverMsg : JSON.stringify(serverMsg), monthlyPayment: 0, interestRatePercent: 0 });
    }
  };

  return (
    <div style={{padding:20}}>
      <h2 style={{textAlign:'center', marginBottom:18}}>Loan Eligibility</h2>

      <div style={{display:'flex', gap:8, alignItems:'center', justifyContent:'center', marginBottom:12}}>
        <input placeholder="Name" value={name} onChange={e=>setName(e.target.value)} style={{minWidth:160, padding:8, borderRadius:4, border:'1px solid #ccc'}} />
        <input type="number" placeholder="Monthly Income" value={income} onChange={e=>setIncome(e.target.value)} style={{width:160, padding:8, borderRadius:4, border:'1px solid #ccc'}} />
        <input type="number" placeholder="Credit Score" value={creditScore} onChange={e=>setCreditScore(e.target.value)} style={{width:120, padding:8, borderRadius:4, border:'1px solid #ccc'}} />
        <input type="number" placeholder="Loan Amount" value={amount} onChange={e=>setAmount(e.target.value)} style={{width:160, padding:8, borderRadius:4, border:'1px solid #ccc'}} />
        <input type="number" placeholder="Term Months" value={term} onChange={e=>setTerm(e.target.value)} style={{width:120, padding:8, borderRadius:4, border:'1px solid #ccc'}} />
        <button onClick={submit} style={{padding:'8px 14px', borderRadius:4, border:'none', background:'#2b6cb0', color:'#fff', cursor:'pointer'}}>Check</button>
      </div>

      {result && (
        <div style={{maxWidth:800, margin:'0 auto', background:'#fff', border:'1px solid #e6e6e6', boxShadow:'0 2px 6px rgba(0,0,0,0.05)', padding:16, borderRadius:6}}>
          <div style={{display:'flex', justifyContent:'space-between', alignItems:'center', marginBottom:8}}>
            <strong>Result</strong>
            <span style={{color: result.eligible ? '#155724' : '#721c24', background: result.eligible ? '#d4edda' : '#f8d7da', padding:'6px 10px', borderRadius:20}}>{result.eligible ? 'Eligible' : 'Not eligible'}</span>
          </div>
          <div style={{display:'flex', gap:20, flexWrap:'wrap', alignItems:'center'}}>
            <div>
              <div style={{fontSize:12, color:'#666'}}>Monthly Payment</div>
              <div style={{fontSize:18, fontWeight:600}}>₹{Number(result.monthlyPayment || 0).toLocaleString(undefined, {minimumFractionDigits:2, maximumFractionDigits:2})}</div>
            </div>
            <div>
              <div style={{fontSize:12, color:'#666'}}>Interest Rate</div>
              <div style={{fontSize:16}}>{Number(result.interestRatePercent ?? result.InterestRatePercent ?? 0).toFixed(2)}%</div>
            </div>
            <div style={{flex:1}}><div style={{fontSize:12, color:'#666'}}>Reason</div><div style={{fontSize:14}}>{result.reason}</div></div>
          </div>
          {/* raw response hidden in UI for cleaner display */}
        </div>
      )}
    </div>
  );
}
