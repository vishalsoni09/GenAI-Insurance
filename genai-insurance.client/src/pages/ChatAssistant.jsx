import { useState, useEffect, useRef } from 'react';
import api from '../services/api';
import './ChatAssistant.css';

export default function ChatAssistant(){
  const [msg, setMsg] = useState('');
  const [conv, setConv] = useState([]);
  const areaRef = useRef(null);
  const [isSending, setIsSending] = useState(false);
  const [, setLastBotTopic] = useState(null);
  const [expectingConfirmation, setExpectingConfirmation] = useState(false);
  const [suggestedTopic, setSuggestedTopic] = useState(null);

  useEffect(()=>{
    // auto-scroll to bottom on new messages
    try { areaRef.current?.scrollTo({ top: areaRef.current.scrollHeight, behavior: 'smooth' }); } catch { /* ignore scroll errors */ }
  }, [conv]);

  // Ensure numbered list items like "1." start on a new line for Markdown rendering
  const formatForMarkdown = (text) => {
    if (!text) return text;
    try {
      // Insert a newline before occurrences of a digit+dot that are not already on their own line
      return text.replace(/([^\n])(?=\s*\n?\s*\d+\.)/g, "$1\n");
    }
    catch { return text; }
  };

  const renderMarkdownToHtml = (text) => {
    if (!text) return '';
    // apply formatting rules
    let t = formatForMarkdown(text);
    // escape HTML
    t = t.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    // bold and italic
    t = t.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
    t = t.replace(/\*(.+?)\*/g, '<em>$1</em>');
    // split into paragraphs on double newline
    const paras = t.split(/\n\n+/).map(p => `<p>${p.trim().replace(/\n/g, '<br/>')}</p>`);
    return paras.join('');
  };

  const send = async ()=>{
    const text = (msg || '').trim();
    if (!text) return;
    // if user replied affirmatively to a suggested topic, expand to a full topic prompt
    const affirmatives = new Set(['yes','y','yeah','sure','ok','please','yep']);
    const negatives = new Set(['no','n','nope','nah']);
    const topicPrompts = {
      health: 'Please provide detailed health insurance information: coverage types, eligibility, exclusions, typical costs, and how it differs from other insurance.',
      car: 'Please provide detailed car loan information: loan amount, tenure, EMI calculation, interest rate ranges, eligibility, and documentation requirements.',
      life: 'Please provide detailed life insurance information: policy types, premiums, beneficiaries, and claim process.'
    };



    // default question to send is the typed text
    let questionToSend = text;
    const low = text.toLowerCase();

    // If we are waiting for confirmation from the user (bot previously suggested a topic)
    if (expectingConfirmation && suggestedTopic) {
      if (affirmatives.has(low) && topicPrompts[suggestedTopic]) {
        questionToSend = topicPrompts[suggestedTopic];
        // consume confirmation
        setExpectingConfirmation(false);
        setSuggestedTopic(null);
      }
      else if (negatives.has(low)) {
        // user declined - clear state and do not call server further
        setExpectingConfirmation(false);
        setSuggestedTopic(null);
        // add user message only and exit
        setConv(prev => [...prev, { from: 'me', text }]);
        setMsg('');
        return;
      }
    }

    const payload = { sessionId: 'default-session', question: questionToSend };

    // optimistic UI: show user's message immediately and a bot placeholder
    const tempBotId = `temp-${Date.now()}`;
    setConv(prev => [...prev, { from: 'me', text }, { from: 'bot', text: '', loading: true, id: tempBotId }]);
    setMsg('');
    setIsSending(true);

    try {
      const resp = await api.post('/api/agent/chat', payload);
      const data = resp.data || {};
      let answer = data.answer ?? data.Answer ?? data.reply ?? '';
      const source = data.source ?? data.Source ?? '';
      const generated = data.generatedSql ?? data.GeneratedSql ?? '';

      // if server returned empty, show friendly message
      if (!answer || !answer.toString().trim()) answer = 'No reply from server';

      // replace placeholder with actual bot message
      setConv(prev => prev.map(m => m.id === tempBotId ? { ...m, loading:false, text: answer, source, generated } : m));

      // Prefer server-provided suggestedTopic (safer than parsing text)
      const suggested = data.suggestedTopic ?? data.SuggestedTopic ?? null;
      if (suggested) {
        setSuggestedTopic(suggested);
        setExpectingConfirmation(true);
        setLastBotTopic(null);
      }
      else {
        const ansLow = (answer || '').toLowerCase();
        if (ansLow.includes('health insurance')) setLastBotTopic('health');
        else if (ansLow.includes('car loan') || ansLow.includes('motor insurance') || ansLow.includes('car')) setLastBotTopic('car');
        else if (ansLow.includes('life insurance')) setLastBotTopic('life');
        else setLastBotTopic(null);
        setExpectingConfirmation(false);
        setSuggestedTopic(null);
      }
    }
    catch (err){
      // fallback attempt
      try {
        const resp2 = await api.post('/api/agent/chat', payload);
        const data2 = resp2.data || {};
        const answer2 = data2.answer ?? data2.Answer ?? data2.reply ?? '';
        const source2 = data2.source ?? data2.Source ?? '';
        const generated2 = data2.generatedSql ?? data2.GeneratedSql ?? '';
        setConv(prev => prev.map(m => m.id === tempBotId ? { ...m, loading:false, text: answer2, source: source2, generated: generated2 } : m));
        const suggested2 = data2.suggestedTopic ?? data2.SuggestedTopic ?? null;
        if (suggested2) {
          setSuggestedTopic(suggested2);
          setExpectingConfirmation(true);
          setLastBotTopic(null);
        }
        else {
          const ansLow2 = (answer2 || '').toLowerCase();
          if (ansLow2.includes('health insurance')) setLastBotTopic('health');
          else if (ansLow2.includes('car loan') || ansLow2.includes('motor insurance') || ansLow2.includes('car')) setLastBotTopic('car');
          else if (ansLow2.includes('life insurance')) setLastBotTopic('life');
          else setLastBotTopic(null);
          setExpectingConfirmation(false);
          setSuggestedTopic(null);
        }
      }
      catch (err2){
        const serverMsg = err?.response?.data ?? err2?.response?.data ?? err?.message ?? 'Error contacting server';
        const textErr = typeof serverMsg === 'string' ? serverMsg : JSON.stringify(serverMsg);
        setConv(prev => prev.map(m => m.id === tempBotId ? { ...m, loading:false, text: textErr } : m));
        setLastBotTopic(null);
      }
    }
    finally {
      setIsSending(false);
    }
  };

  return (
    <div className="chat-container">
      <div className="chat-card">
        <div className="chat-header">
          <div className="tabs">
            <button className="tab active">Chat</button>
          </div>
          <div className="header-actions">●</div>
        </div>

        <div className="chat-area" ref={areaRef}>
          {conv.length === 0 ? (
            <div className="placeholder">
              <div className="logo">⟲</div>
              <div className="prompt">What do you want to chat about?</div>
            </div>
          ) : (
            conv.map((c,i)=>(
              <div key={i} className={`message ${c.from === 'me' ? 'me' : 'bot'}`}>
                <div className="meta"><b>{c.from}:</b></div>
                <div className="text">
                  {c.loading ? (
                    <span className="typing"><span className="dot"></span><span className="dot"></span><span className="dot"></span></span>
                  ) : (
                    // Render bot content as sanitized HTML produced from simple Markdown rules
                    <div className="markdown" dangerouslySetInnerHTML={{ __html: renderMarkdownToHtml(c.text) }} />
                  )}
                </div>
                {c.source && (<div className="source">Source: {c.source}</div>)}
                {c.generated && (<pre className="generated">{c.generated}</pre>)}
              </div>
            ))
          )}
        </div>

        <div className="chat-input">
          <textarea
            value={msg}
            onChange={e=>setMsg(e.target.value)}
            placeholder="Chat with the model..."
            onKeyDown={e=>{ if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); } }}
            />
          <button className="send-btn" onClick={send} aria-label="Send" disabled={isSending}>
            {isSending ? <span className="typing" style={{width:40}}><span className="dot"></span><span className="dot"></span><span className="dot"></span></span> : '➤'}
          </button>
        </div>
      </div>
    </div>
  );
}
