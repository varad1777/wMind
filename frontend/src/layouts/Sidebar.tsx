import { NavLink } from "react-router-dom";
import { SIDEBAR_ITEMS } from "../sidebar/sidebarItems";
import { ROLE_SIDEBAR_ACCESS } from "../sidebar/roleSidebarConfig";
import { useAuth } from "@/context/AuthContext";

export default function Sidebar() {
  const { user } = useAuth();
  const role = user?.role ?? "Viewer";

  const allowedKeys = ROLE_SIDEBAR_ACCESS[role] ?? [];

  const visibleItems = SIDEBAR_ITEMS.filter(item =>
    allowedKeys.includes(item.key)
  );

  return (
    <aside id="sidebar" className="sticky top-0 z-40 w-64 bg-sidebar text-sidebar-foreground border-r border-sidebar-border flex flex-col p-4 rounded-sm">
      <div className="h-12 flex items-center justify-center border-b border-sidebar-border px-4 mb-6">
        <div className="flex items-center gap-2">
          <div className="w-10 h-10  bg-sidebar pb-2 rounded flex items-center justify-center font-bold text-sidebar-foreground">
            <img className="rounded" src="https://www.clipartmax.com/png/middle/151-1514067_tata-logo-png.png" alt="" />
          </div>
          <span className="font-bold text-2xl mb-2 text-sidebar-foreground">Tmind</span>
        </div>
      </div>

       <nav className="space-y-2">
        {visibleItems.map((item) => {
          const Icon = item.icon;

          return (
            <NavLink
              key={item.key}
              to={item.path}
              id={`sidebar-${item.key}`}
              className={({ isActive }) =>
                `flex items-center gap-3 p-2 rounded-lg transition ${
                  isActive
                    ? "bg-sidebar-primary text-sidebar-primary-foreground"
                    : "hover:bg-sidebar-primary hover:text-sidebar-primary-foreground"
                }`
              }
            >
              <Icon size={18} />
              <span>{item.label}</span>
            </NavLink>
          );
        })}
      </nav>
    </aside>
  );
}
