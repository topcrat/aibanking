import client from './client';

export interface LoginPayload {
  username: string;
  password: string;
}

export interface LoginResult {
  token:    string;
  userId:   string;
  username: string;
  fullName: string;
  role:     string;
}

export async function login(payload: LoginPayload): Promise<LoginResult> {
  const res = await client.post<LoginResult>('/auth/login', payload);
  return res.data;
}

export interface CreateUserPayload {
  username: string;
  password: string;
  fullName: string;
  role:     string;
}

export interface UserRecord {
  id:          string;
  username:    string;
  fullName:    string;
  role:        string;
  isActive:    boolean;
  createdAt:   string;
  lastLoginAt: string | null;
}

export const usersApi = {
  list: () =>
    client.get<UserRecord[]>('/auth/users').then(r => r.data),

  create: (payload: CreateUserPayload) =>
    client.post<UserRecord>('/auth/users', payload).then(r => r.data),

  changePassword: (id: string, newPassword: string) =>
    client.put(`/auth/users/${id}/password`, { newPassword }),

  setActive: (id: string, isActive: boolean) =>
    client.put(`/auth/users/${id}/active`, { isActive }),
};
