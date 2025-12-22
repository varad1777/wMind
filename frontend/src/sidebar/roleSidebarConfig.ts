export type UserRole = "Admin" | "Engineer" | "Operator" | "User";

export const ROLE_SIDEBAR_ACCESS: Record<UserRole, string[]> = {
  Admin: [
    "dashboard",
    "assets",
    "devices",
    "signal",
    "reports",
    "notifications",
    "manage-user",
    "deleted-items",
  ],

  Engineer: [
    "dashboard",
    "assets",
    "devices",
    "signal",
    "notifications",
  ],

  Operator: [
    "dashboard",
    "assets",
    "signal",
    "notifications",
    "reports",
  ],

  User: [
    "dashboard",
    "reports",
  ],
};
