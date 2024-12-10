
-- iff > 0 #v_dept_pid
	-- set
	SELECT eox_dept.* FROM eox_dept, eox_dept_index 
		WHERE eox_dept._update_flag>0 
		AND eox_dept.f_dept_id = eox_dept_index.f_dept_id 
		AND eox_dept_index.f_dept_pid = #v_dept_pid
		ORDER BY eox_dept.f_dept_pid, eox_dept.f_dept_id;
	-- end
-- end

-- iff <= 0 #v_dept_pid
	-- set
	SELECT eox_dept.* FROM eox_dept
		WHERE eox_dept._update_flag > 0 
		ORDER BY eox_dept.f_dept_pid, eox_dept.f_dept_id;
	-- end
-- end