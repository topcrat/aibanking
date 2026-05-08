import { useState, type FormEvent } from 'react';
import type { FormDefinition, FormFieldDefinition } from '../types';

interface Props {
  definition: FormDefinition;
  onSubmit: (values: Record<string, string>, files: Record<string, File>) => Promise<void>;
  loading?: boolean;
}

export default function DynamicForm({ definition, onSubmit, loading }: Props) {
  const [values, setValues] = useState<Record<string, string>>({});
  const [files,  setFiles]  = useState<Record<string, File>>({});
  const [errors, setErrors] = useState<Record<string, string>>();

  function set(key: string, value: string) {
    setValues(v => ({ ...v, [key]: value }));
    setErrors(e => { const next = { ...(e ?? {}) }; delete next[key]; return next; });
  }

  function setFile(key: string, file: File) {
    setFiles(f => ({ ...f, [key]: file }));
    setValues(v => ({ ...v, [key]: file.name }));
    setErrors(e => { const next = { ...(e ?? {}) }; delete next[key]; return next; });
  }

  function validate() {
    const next: Record<string, string> = {};
    for (const field of definition.fields) {
      if (!field.isRequired) continue;
      if (field.fieldType === 'File') {
        if (!files[field.fieldKey]) next[field.fieldKey] = `${field.label} is required.`;
      } else {
        if (!values[field.fieldKey]?.trim()) next[field.fieldKey] = `${field.label} is required.`;
      }
    }
    setErrors(next);
    return Object.keys(next).length === 0;
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!validate()) return;
    await onSubmit(values, files);
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      {definition.fields.map(field => (
        <FieldRenderer
          key={field.id}
          field={field}
          value={values[field.fieldKey] ?? ''}
          error={errors?.[field.fieldKey]}
          onChange={v => set(field.fieldKey, v)}
          onFile={f => setFile(field.fieldKey, f)}
        />
      ))}

      <button
        type="submit"
        disabled={loading}
        className="w-full py-2.5 bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white font-medium rounded-lg text-sm transition-colors"
      >
        {loading ? 'Submitting…' : 'Submit'}
      </button>
    </form>
  );
}

interface FieldProps {
  field: FormFieldDefinition;
  value: string;
  error?: string;
  onChange: (v: string) => void;
  onFile: (f: File) => void;
}

function FieldRenderer({ field, value, error, onChange, onFile }: FieldProps) {
  const options: string[] = field.optionsJson ? JSON.parse(field.optionsJson) : [];

  const baseClass =
    'w-full px-3 py-2 border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 ' +
    (error ? 'border-red-400 bg-red-50' : 'border-gray-300');

  const label = (
    <label className="block text-sm font-medium text-gray-700 mb-1">
      {field.label}
      {field.isRequired && <span className="text-red-500 ml-0.5">*</span>}
    </label>
  );

  let input: React.ReactNode;

  switch (field.fieldType) {
    case 'TextArea':
      input = (
        <textarea
          rows={3}
          value={value}
          onChange={e => onChange(e.target.value)}
          placeholder={field.placeholder ?? ''}
          className={baseClass}
        />
      );
      break;

    case 'Date':
      input = (
        <input
          type="date"
          value={value}
          onChange={e => onChange(e.target.value)}
          className={baseClass}
        />
      );
      break;

    case 'Number':
      input = (
        <input
          type="number"
          value={value}
          onChange={e => onChange(e.target.value)}
          placeholder={field.placeholder ?? ''}
          className={baseClass}
        />
      );
      break;

    case 'Select':
      input = (
        <select
          value={value}
          onChange={e => onChange(e.target.value)}
          className={baseClass}
        >
          <option value="">Select…</option>
          {options.map(o => <option key={o} value={o}>{o}</option>)}
        </select>
      );
      break;

    case 'File':
      input = (
        <div>
          <input
            type="file"
            accept="image/jpeg,image/png,image/webp,application/pdf"
            onChange={e => { const f = e.target.files?.[0]; if (f) onFile(f); }}
            className={baseClass + ' file:mr-3 file:py-1 file:px-3 file:rounded file:border-0 file:text-sm file:bg-blue-50 file:text-blue-700'}
          />
          {value && <p className="text-xs text-blue-600 mt-1">Selected: {value}</p>}
        </div>
      );
      break;

    default:
      input = (
        <input
          type="text"
          value={value}
          onChange={e => onChange(e.target.value)}
          placeholder={field.placeholder ?? ''}
          className={baseClass}
        />
      );
  }

  return (
    <div>
      {label}
      {input}
      {error && <p className="mt-1 text-xs text-red-600">{error}</p>}
    </div>
  );
}
