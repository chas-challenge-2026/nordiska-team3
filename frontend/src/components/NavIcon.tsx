import type { ReactNode } from "react";

interface NavIconProps {
  icon: ReactNode;
  label: string;
  active?: boolean;
  onClick?: () => void;
}

export function NavIcon({ icon, label, active = false, onClick }: NavIconProps) {
  return (
    <button
      onClick={onClick}
      aria-label={label}
      aria-current={active ? "page" : undefined}
      className={`nav-icon ${active ? "nav-icon--active" : ""}`}
    >
      {icon}
    </button>
  );
}