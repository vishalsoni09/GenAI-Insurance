import React from 'react';
import LoanEligibility from './pages/LoanEligibility';
import ChatAssistant from './pages/ChatAssistant';

export default function App(){
  return (
    <div>
      <h1>Loan POC UI</h1>
      <LoanEligibility />
      <ChatAssistant />
    </div>
  );
}
