import client from './client';
import type { FormDefinition, FormSubmission, FormFieldType } from '../types';

export interface CreateFormDefinitionPayload {
  name: string;
  description?: string;
  workflowDefinitionId: string;
  fields: {
    fieldOrder: number;
    fieldKey: string;
    label: string;
    fieldType: FormFieldType;
    isRequired: boolean;
    placeholder?: string;
    options?: string[];
  }[];
}

export const formsApi = {
  listDefinitions: () =>
    client.get<FormDefinition[]>('/form/definitions').then(r => r.data),

  getDefinition: (id: string) =>
    client.get<FormDefinition>(`/form/definitions/${id}`).then(r => r.data),

  createDefinition: (payload: CreateFormDefinitionPayload) =>
    client.post<FormDefinition>('/form/definitions', payload).then(r => r.data),

  submit: (formDefinitionId: string, values: Record<string, string>) =>
    client.post<{ submission: FormSubmission; workflowItem: unknown }>(
      '/form/submit',
      { formDefinitionId, values }
    ).then(r => r.data),

  getSubmission: (id: string) =>
    client.get<FormSubmission>(`/form/submissions/${id}`).then(r => r.data),

  getSubmissionByWorkflow: (workflowItemId: string) =>
    client.get<FormSubmission>(`/form/submissions/by-workflow/${workflowItemId}`).then(r => r.data),
};
