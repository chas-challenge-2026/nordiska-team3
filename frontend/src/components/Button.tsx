import type { ButtonHTMLAttributes, ReactNode } from "react";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode;
  variant?: "primary" | "secondary";
}

export function Button({ children, variant = "primary", className, ...props }: ButtonProps) {
  const variantClass = variant === "primary" ? "button--primary" : "button--secondary";

  return (
    <button className={`button ${variantClass} ${className ?? ""}`} {...props}>
      {children}
    </button>
  );
}