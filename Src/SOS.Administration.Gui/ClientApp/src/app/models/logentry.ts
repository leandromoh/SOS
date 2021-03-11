export interface LogEntry {
  level: string;
  message: string;
  timestamp: Date;
  hostname: string;
  processname: number
}
