import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Send, Bot, User, Trash2, Sparkles } from 'lucide-react';
import { agentApi } from '../api/agent';
import type { ChatMessage } from '../types';

const SUGGESTIONS = [
  'List all applications under review',
  'Show me pending workflows',
  'Process application and run BVN verification',
  'What are the onboarding KPI metrics?',
  'Show fraud assessment summary',
];

export default function AgentChat() {
  const [searchParams]    = useSearchParams();
  const appId             = searchParams.get('appId');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input,    setInput]    = useState('');
  const [loading,  setLoading]  = useState(false);
  const [convId,   setConvId]   = useState<string | undefined>();
  const bottomRef = useRef<HTMLDivElement>(null);

  // Pre-fill if launched from application detail
  useEffect(() => {
    if (appId) {
      setInput(`Please review and process application ${appId} — run standards check, BVN verification, fraud assessment, and if everything passes, create the customer and account.`);
    }
  }, [appId]);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, loading]);

  async function sendMessage(text?: string) {
    const msg = (text ?? input).trim();
    if (!msg || loading) return;

    setInput('');
    const userMsg: ChatMessage = { role: 'user', content: msg, timestamp: new Date() };
    setMessages(prev => [...prev, userMsg]);
    setLoading(true);

    try {
      const res = await agentApi.chat(msg, convId);
      setConvId(res.conversationId);
      setMessages(prev => [...prev, {
        role: 'assistant',
        content: res.reply,
        timestamp: new Date()
      }]);
    } catch {
      setMessages(prev => [...prev, {
        role: 'assistant',
        content: 'Sorry, I encountered an error. Please try again.',
        timestamp: new Date()
      }]);
    } finally {
      setLoading(false);
    }
  }

  async function clearConversation() {
    if (convId) {
      try { await agentApi.clearConversation(convId); } catch { /* ignore */ }
    }
    setMessages([]);
    setConvId(undefined);
    setInput('');
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header */}
      <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 bg-white shrink-0">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center">
            <Bot size={18} className="text-white" />
          </div>
          <div>
            <h1 className="font-semibold text-gray-900">AI Banking Agent</h1>
            <p className="text-xs text-gray-400">
              {convId ? `Session: ${convId.slice(0, 8)}…` : 'New session'}
            </p>
          </div>
        </div>
        {messages.length > 0 && (
          <button
            onClick={clearConversation}
            className="flex items-center gap-1.5 text-sm text-gray-500 hover:text-red-600 transition-colors"
          >
            <Trash2 size={15} />
            Clear
          </button>
        )}
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-y-auto px-6 py-6 space-y-5">
        {messages.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-full text-center pb-20">
            <div className="w-16 h-16 rounded-2xl bg-blue-100 flex items-center justify-center mb-4">
              <Sparkles size={28} className="text-blue-600" />
            </div>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">How can I help you?</h2>
            <p className="text-sm text-gray-500 max-w-sm mb-8">
              I can review applications, run BVN verifications, fraud assessments,
              manage workflows, and process accounts end-to-end.
            </p>
            <div className="grid grid-cols-1 gap-2 w-full max-w-lg">
              {SUGGESTIONS.map(s => (
                <button
                  key={s}
                  onClick={() => sendMessage(s)}
                  className="px-4 py-2.5 text-left text-sm bg-white border border-gray-200 rounded-xl hover:border-blue-400 hover:bg-blue-50 text-gray-700 transition-colors"
                >
                  {s}
                </button>
              ))}
            </div>
          </div>
        ) : (
          messages.map((msg, i) => (
            <MessageBubble key={i} message={msg} />
          ))
        )}

        {loading && (
          <div className="flex items-start gap-3">
            <div className="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center shrink-0">
              <Bot size={15} className="text-white" />
            </div>
            <div className="bg-white border border-gray-200 rounded-2xl rounded-tl-sm px-4 py-3 shadow-sm">
              <div className="flex items-center gap-1.5">
                <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                <span className="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
              </div>
            </div>
          </div>
        )}
        <div ref={bottomRef} />
      </div>

      {/* Input */}
      <div className="shrink-0 border-t border-gray-200 bg-white px-6 py-4">
        <div className="flex items-end gap-3 bg-gray-50 border border-gray-200 rounded-2xl px-4 py-3 focus-within:border-blue-400 focus-within:bg-white transition-colors">
          <textarea
            value={input}
            onChange={e => setInput(e.target.value)}
            onKeyDown={e => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
              }
            }}
            rows={Math.min(4, Math.max(1, input.split('\n').length))}
            placeholder="Ask me anything about applications, workflows, BVN, fraud, or digital enrollment…"
            className="flex-1 bg-transparent text-sm text-gray-900 placeholder-gray-400 resize-none focus:outline-none"
            disabled={loading}
          />
          <button
            onClick={() => sendMessage()}
            disabled={!input.trim() || loading}
            className="shrink-0 w-8 h-8 bg-blue-600 hover:bg-blue-700 disabled:opacity-40 text-white rounded-xl flex items-center justify-center transition-colors"
          >
            <Send size={15} />
          </button>
        </div>
        <p className="text-xs text-gray-400 mt-2 text-center">
          Press Enter to send · Shift+Enter for new line
        </p>
      </div>
    </div>
  );
}

function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';

  return (
    <div className={`flex items-start gap-3 ${isUser ? 'flex-row-reverse' : ''}`}>
      <div className={`w-8 h-8 rounded-full flex items-center justify-center shrink-0 ${
        isUser ? 'bg-slate-700' : 'bg-blue-600'
      }`}>
        {isUser ? <User size={15} className="text-white" /> : <Bot size={15} className="text-white" />}
      </div>
      <div className={`max-w-[75%] ${isUser ? 'items-end' : 'items-start'} flex flex-col gap-1`}>
        <div className={`px-4 py-3 rounded-2xl text-sm shadow-sm ${
          isUser
            ? 'bg-slate-800 text-white rounded-tr-sm'
            : 'bg-white border border-gray-200 text-gray-900 rounded-tl-sm'
        }`}>
          <MessageContent content={message.content} />
        </div>
        <p className="text-xs text-gray-400 px-1">
          {message.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
        </p>
      </div>
    </div>
  );
}

function MessageContent({ content }: { content: string }) {
  // Render markdown-style formatting: **bold**, `code`, newlines
  const lines = content.split('\n');
  return (
    <div className="space-y-1.5 whitespace-pre-wrap break-words leading-relaxed">
      {lines.map((line, i) => {
        if (!line.trim()) return <br key={i} />;
        return <p key={i}>{formatInline(line)}</p>;
      })}
    </div>
  );
}

function formatInline(text: string) {
  // Very simple inline **bold** and `code` rendering
  const parts = text.split(/(\*\*[^*]+\*\*|`[^`]+`)/g);
  return parts.map((part, i) => {
    if (part.startsWith('**') && part.endsWith('**')) {
      return <strong key={i}>{part.slice(2, -2)}</strong>;
    }
    if (part.startsWith('`') && part.endsWith('`')) {
      return <code key={i} className="bg-gray-100 text-gray-800 px-1 rounded text-xs font-mono">{part.slice(1, -1)}</code>;
    }
    return part;
  });
}
