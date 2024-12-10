
---- 根据角色编号字符串获取权限清单

-- set
SELECT f_permits FROM eox_role 
	WHERE _update_flag>0 AND f_role_id IN (#v_role_ids)
-- end

