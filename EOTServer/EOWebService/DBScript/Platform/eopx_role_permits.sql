

---- 更新角色权限字符串

-- set
UPDATE eox_role 
	SET f_permits = '#v_permits', _update_time = ##now
	WHERE f_role_id = #v_role_id;
-- end

-- set
select 0 AS _d, '' AS _s, #v_role_id AS f_role_id;
-- end