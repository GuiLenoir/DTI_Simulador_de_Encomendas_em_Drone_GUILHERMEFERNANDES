import { useEffect, useRef } from "react";

export function usePolling(callback: () => Promise<void>, intervalMs: number) {
  const callbackRef = useRef(callback);
  const isRunningRef = useRef(false);

  useEffect(() => {
    callbackRef.current = callback;
  }, [callback]);

  useEffect(() => {
    let isMounted = true;

    async function tick() {
      if (isRunningRef.current || !isMounted) {
        return;
      }

      isRunningRef.current = true;
      try {
        await callbackRef.current();
      } finally {
        isRunningRef.current = false;
      }
    }

    void tick();
    const intervalId = window.setInterval(() => void tick(), intervalMs);

    return () => {
      isMounted = false;
      window.clearInterval(intervalId);
    };
  }, [intervalMs]);
}
