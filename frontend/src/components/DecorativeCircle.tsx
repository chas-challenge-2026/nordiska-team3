interface DecorativeCircleProps {
  color: "orange" | "blue" | "green" | "white";
  size: number;
  top?: number | string;
  left?: number | string;
  right?: number | string;
  bottom?: number | string;
  opacity?: number;
}

export function DecorativeCircle({
  color,
  size,
  top,
  left,
  right,
  bottom,
  opacity = 1,
}: DecorativeCircleProps) {
  return (
    <div
      className={`decorative-circle decorative-circle--${color}`}
      style={{
        width: size,
        height: size,
        top,
        left,
        right,
        bottom,
        opacity,
      }}
    />
  );
}