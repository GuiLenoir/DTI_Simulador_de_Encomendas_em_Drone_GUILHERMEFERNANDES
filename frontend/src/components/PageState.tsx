type PageStateProps = {
  message: string;
};

export function LoadingState({ message }: PageStateProps) {
  return <div className="state state-muted">{message}</div>;
}

export function EmptyState({ message }: PageStateProps) {
  return <div className="state state-muted">{message}</div>;
}

export function ErrorState({ message }: PageStateProps) {
  return <div className="state state-error">{message}</div>;
}
