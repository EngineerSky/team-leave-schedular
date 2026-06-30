export interface Team {
  id: number;
  name: string;
}

export interface Employee {
  id: number;
  name: string;
  teamId: number;
  teamName: string;
  leaveBalance: number;
}

export interface EmployeeLeaveDto {
  employeeId: number;
  employeeName: string;
}

export interface CalendarDay {
  date: string;
  isWorkingDay: boolean;
  allowedLimit: number;
  employeesOnLeave: EmployeeLeaveDto[];
}

export interface TeamCalendar {
  teamId: number;
  teamName: string;
  teamSize: number;
  allowedLimit: number;
  days: CalendarDay[];
}

export interface LeaveRequest {
  id: number;
  employeeId: number;
  employeeName: string;
  startDate: string;
  endDate: string;
  status: string;
  statusReason: string | null;
}
