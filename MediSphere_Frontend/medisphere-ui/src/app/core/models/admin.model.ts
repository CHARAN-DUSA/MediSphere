export interface DashboardStats {
  totalAppointments: number;
  todayAppointments: number;
  totalDoctors: number;
  totalPatients: number;
  totalDepartments: number;
  totalRevenue: number;
  pendingAppointments: number;
  completedAppointments: number;
  departmentStats: DepartmentStat[];
}

export interface DepartmentStat {
  departmentName: string;
  appointmentCount: number;
}

export interface SystemSetting {
  key: string;
  value: string;
  description: string;
}

export interface ContentItem {
  id: number;
  type: string; // FAQ, HealthArticle, Banner
  title: string;
  content: string;
  imageUrl: string;
  order: number;
}
