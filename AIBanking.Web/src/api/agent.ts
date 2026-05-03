import client from './client';
import type { ChatResponse } from '../types';

export const agentApi = {
  chat: (message: string, conversationId?: string) =>
    client
      .post<ChatResponse>('/agent/chat', { message, conversationId })
      .then((r) => r.data),

  clearConversation: (conversationId: string) =>
    client.delete(`/agent/conversations/${conversationId}`),
};
