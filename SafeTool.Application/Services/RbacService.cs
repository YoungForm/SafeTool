namespace SafeTool.Application.Services;

/// <summary>
/// RBAC权限管理服务（策略模式）
/// </summary>
public class RbacService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private RbacData _data = new();

    public RbacService(string dataDir)
    {
        var dir = Path.Combine(dataDir, "RBAC");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "rbac.json");
        Load();
        InitializeDefaultRoles();
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            var data = System.Text.Json.JsonSerializer.Deserialize<RbacData>(
                json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data != null)
                _data = data;
        }
    }

    private void Save()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(_data,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private void InitializeDefaultRoles()
    {
        if (_data.Roles.Count > 0) return;

        // 默认角色
        _data.Roles.Add(new Role
        {
            Id = "engineer",
            Name = "安全工程师",
            Description = "可以创建和编辑评估、SRS、变更请求",
            Permissions = new List<string> { "assessment:create", "assessment:edit", "srs:create", "srs:edit", "changerequest:create", "changerequest:edit" }
        });

        _data.Roles.Add(new Role
        {
            Id = "reviewer",
            Name = "审核员",
            Description = "可以审核和审批SRS、变更请求",
            Permissions = new List<string> { "srs:review", "srs:approve", "changerequest:review", "changerequest:approve", "report:view" }
        });

        _data.Roles.Add(new Role
        {
            Id = "admin",
            Name = "管理员",
            Description = "拥有所有权限",
            Permissions = new List<string> { "*" } // 通配符表示所有权限
        });

        Save();
    }

    public bool HasPermission(string userId, string permission)
    {
        lock (_lock)
        {
            // 获取用户角色
            if (!_data.UserRoles.TryGetValue(userId, out var roleIds))
                return false;

            foreach (var roleId in roleIds)
            {
                var role = _data.Roles.FirstOrDefault(r => r.Id == roleId);
                if (role == null) continue;

                // 检查通配符权限
                if (role.Permissions.Contains("*"))
                    return true;

                // 检查具体权限
                if (role.Permissions.Contains(permission))
                    return true;

                // 检查权限前缀（如 assessment:*）
                var prefix = permission.Split(':')[0] + ":*";
                if (role.Permissions.Contains(prefix))
                    return true;
            }

            return false;
        }
    }

    public IEnumerable<string> GetUserPermissions(string userId)
    {
        lock (_lock)
        {
            if (!_data.UserRoles.TryGetValue(userId, out var roleIds))
                return Enumerable.Empty<string>();

            var permissions = new HashSet<string>();
            foreach (var roleId in roleIds)
            {
                var role = _data.Roles.FirstOrDefault(r => r.Id == roleId);
                if (role == null) continue;

                foreach (var perm in role.Permissions)
                {
                    if (perm == "*")
                    {
                        permissions.Add("*");
                        break;
                    }
                    permissions.Add(perm);
                }
            }

            return permissions;
        }
    }

    public void AssignRole(string userId, string roleId)
    {
        lock (_lock)
        {
            if (!_data.UserRoles.TryGetValue(userId, out var roleIds))
            {
                roleIds = new List<string>();
                _data.UserRoles[userId] = roleIds;
            }

            if (!roleIds.Contains(roleId))
            {
                roleIds.Add(roleId);
                Save();
            }
        }
    }

    public void RemoveRole(string userId, string roleId)
    {
        lock (_lock)
        {
            if (_data.UserRoles.TryGetValue(userId, out var roleIds))
            {
                roleIds.Remove(roleId);
                if (roleIds.Count == 0)
                    _data.UserRoles.Remove(userId);
                Save();
            }
        }
    }

    public IEnumerable<Role> GetRoles()
    {
        lock (_lock)
        {
            return _data.Roles.AsEnumerable();
        }
    }

    public Role? GetRole(string roleId)
    {
        lock (_lock)
        {
            return _data.Roles.FirstOrDefault(r => r.Id == roleId);
        }
    }

    public Role CreateRole(Role role)
    {
        lock (_lock)
        {
            role.Id = role.Id ?? Guid.NewGuid().ToString("N");
            _data.Roles.Add(role);
            Save();
            return role;
        }
    }

    public class Role
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
    }

    private class RbacData
    {
        public List<Role> Roles { get; set; } = new();
        public Dictionary<string, List<string>> UserRoles { get; set; } = new(); // userId -> roleIds
    }
}

