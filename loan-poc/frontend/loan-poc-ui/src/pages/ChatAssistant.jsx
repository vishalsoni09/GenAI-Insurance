import React, { useState } from 'react';
import api from '../services/api';

export default function ChatAssistant(){
  const [msg, setMsg] = useState('');
  const [conv, setConv] = useState([]);

  const send = async ()=>{
    const req = { userId: 'user1', message: msg };
    const resp = await api.post('/chat/message', req);
    setConv(c=>[...c, { from: 'me', text: msg }, { from: 'bot', text: resp.data.reply }]);
    setMsg('');
  };

  return (
    <div>
      <h2>Chat Assistant</h2>
      <div>
        {conv.map((c,i)=>(<div key={i}><b>{c.from}:</b> {c.text}</div>))}
      </div>
      <input value={msg} onChange={e=>setMsg(e.target.value)} />
      <button onClick={send}>Send</button>
    </div>
  );
}
