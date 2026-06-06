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
    const resp = await api.post('/loan/assess', req);
    setResult(resp.data);
  };

  return (
    <div>
      <h2>Loan Eligibility</h2>
      <input placeholder="Name" value={name} onChange={e=>setName(e.target.value)} />
      <input placeholder="Monthly Income" value={income} onChange={e=>setIncome(e.target.value)} />
      <input placeholder="Credit Score" value={creditScore} onChange={e=>setCreditScore(e.target.value)} />
      <input placeholder="Loan Amount" value={amount} onChange={e=>setAmount(e.target.value)} />
      <input placeholder="Term Months" value={term} onChange={e=>setTerm(e.target.value)} />
      <button onClick={submit}>Check</button>
      {result && (
        <div>
          <p>Eligible: {result.eligible ? 'Yes' : 'No'}</p>
          <p>Monthly Payment: {result.monthlyPayment}</p>
          <p>Reason: {result.reason}</p>
        </div>
      )}
    </div>
  );
}
