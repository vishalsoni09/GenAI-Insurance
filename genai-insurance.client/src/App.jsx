import { useState } from 'react';
import './App.css';
import LoanEligibility from './pages/LoanEligibility';
import ChatAssistant from './pages/ChatAssistant';

function App(){
    const [page, setPage] = useState('home');

    return (
        <div>
            <h1>GenAI Insurance</h1>
            <nav>
                <button onClick={()=>setPage('home')}>Home</button>
                <button onClick={()=>setPage('loan')}>Loan Eligibility</button>
                <button onClick={()=>setPage('chat')}>Chat Assistant</button>
            </nav>

            {page === 'home' && (
                <div>
                    <h2>Welcome</h2>
                    <p>Use the navigation to open the POC pages.</p>
                </div>
            )}

            {page === 'loan' && <LoanEligibility />}
            {page === 'chat' && <ChatAssistant />}
        </div>
    );
}

export default App;
