import client from './client';
import type {
  AccountApplication,
  AccountStatus,
  Customer,
  BankAccount,
  DocumentType,
  ExtractedPersonInfo,
  ProcessSummary,
} from '../types';

export const applicationsApi = {
  create: () =>
    client.post<AccountApplication>('/account/applications').then((r) => r.data),

  list: (status?: AccountStatus) =>
    client
      .get<AccountApplication[]>('/account/applications', { params: status ? { status } : {} })
      .then((r) => r.data),

  getById: (id: string) =>
    client.get<AccountApplication>(`/account/applications/${id}`).then((r) => r.data),

  uploadDocument: (id: string, file: File, documentType: DocumentType) => {
    const form = new FormData();
    form.append('file', file);
    form.append('documentType', documentType);
    return client
      .post(`/account/applications/${id}/documents`, form)
      .then((r) => r.data);
  },

  extract: (id: string) =>
    client.post<AccountApplication>(`/account/applications/${id}/extract`).then((r) => r.data),

  getExtractedInfo: (id: string) =>
    client
      .get<ExtractedPersonInfo>(`/account/applications/${id}/extracted-info`)
      .then((r) => r.data),

  getProcesses: (id: string) =>
    client
      .get<ProcessSummary[]>(`/account/applications/${id}/processes`)
      .then((r) => r.data),

  createCustomer: (id: string) =>
    client
      .post<{ message: string; customer: Customer }>(
        `/account/applications/${id}/processes/create-customer`
      )
      .then((r) => r.data),

  createAccount: (id: string, accountType: string = 'Savings') =>
    client
      .post<{ message: string; account: BankAccount }>(
        `/account/applications/${id}/processes/create-account`,
        { accountType }
      )
      .then((r) => r.data),
};
