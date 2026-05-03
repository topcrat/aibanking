import client from './client';
import type { WorkflowItem, WorkflowStatus } from '../types';

export interface SubmitWorkflowPayload {
  definitionId: string;
  title: string;
  description?: string;
}

export interface WorkflowActionPayload {
  comments?: string;
}

export const workflowsApi = {
  submit: (payload: SubmitWorkflowPayload) =>
    client.post<WorkflowItem>('/workflow', payload).then(r => r.data),

  list: (status?: WorkflowStatus) =>
    client.get<WorkflowItem[]>('/workflow', { params: status ? { status } : {} }).then(r => r.data),

  getById: (id: string) =>
    client.get<WorkflowItem>(`/workflow/${id}`).then(r => r.data),

  approve: (id: string, payload: WorkflowActionPayload) =>
    client.post<WorkflowItem>(`/workflow/${id}/approve`, payload).then(r => r.data),

  rework: (id: string, payload: WorkflowActionPayload) =>
    client.post<WorkflowItem>(`/workflow/${id}/rework`, payload).then(r => r.data),

  decline: (id: string, payload: WorkflowActionPayload) =>
    client.post<WorkflowItem>(`/workflow/${id}/decline`, payload).then(r => r.data),

  resubmit: (id: string) =>
    client.post<WorkflowItem>(`/workflow/${id}/resubmit`, {}).then(r => r.data),

  listDocuments: (id: string) =>
    client.get(`/workflow/${id}/documents`).then(r => r.data),

  uploadDocument: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return client.post(`/workflow/${id}/documents`, form).then(r => r.data);
  },

  deleteDocument: (id: string, documentId: string) =>
    client.delete(`/workflow/${id}/documents/${documentId}`),
};
