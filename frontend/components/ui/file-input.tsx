import * as React from "react";
import { cn } from "@/lib/utils";

export type FileInputProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, "type">;

const FileInput = React.forwardRef<HTMLInputElement, FileInputProps>(({ className, ...props }, ref) => {
  return (
    <input
      ref={ref}
      type="file"
      className={cn(
        "flex h-10 w-full cursor-pointer rounded-md border border-border bg-white px-3 py-1.5 text-sm text-slate-900 file:mr-3 file:rounded-md file:border-0 file:bg-slate-100 file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-slate-800 hover:file:bg-slate-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50",
        className
      )}
      {...props}
    />
  );
});

FileInput.displayName = "FileInput";

export { FileInput };
