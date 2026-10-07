import type { ReactNode } from "react";

type CardColor = "white" | "mint" | "peach" | "dark";
type CardSize = "lg" | "md" | "sm";

interface BlobCardProps {
  color?: CardColor;
  size?: CardSize;
  icon?: ReactNode;
  label: string;
  value: string;
  sublabel?: string;
}

export function BlobCard({ color = "white", size = "md", icon, label, value, sublabel }: BlobCardProps) {
  return (
    <div className={`blob-card blob-card--${color} blob-card--${size}`}>
      {icon && <div className="blob-card-icon">{icon}</div>}
      <p className="blob-card-label">{label}</p>
      <p className="blob-card-value">{value}</p>
      {sublabel && <p className="blob-card-sublabel">{sublabel}</p>}
    </div>
  );
}