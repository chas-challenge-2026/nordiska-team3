import type { InputHTMLAttributes } from "react";

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  type?: "text" | "amount" | "password" | "email";
}

export function Input({ label, error, type = "text", className, ...props }: InputProps) {
  const isAmount = type === "amount";
  const inputType = isAmount ? "text" : type;

  return (
    <div className="input-group">
      <label className="input-label">{label}</label>
      <div className="input-wrapper">
        <input
          type={inputType}
          className={`input ${error ? "input--error" : ""} ${className ?? ""}`}
          {...props}
        />
        {isAmount && <span className="input-suffix">kr</span>}
      </div>
      {error && <p className="input-error-text">{error}</p>}
    </div>
  );
}