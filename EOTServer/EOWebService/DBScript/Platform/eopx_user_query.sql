---- v_dept_id
---- 查询部门所有账户信息

-- set
select eox_user.*, 
	'' as f_login_psw, 
	eox_dept.f_name as f_dept_id_s
	from eox_user, eox_dept_index, eox_dept
	where eox_user.f_dept_id = eox_dept_index.f_dept_id
    and eox_user.f_dept_id = eox_dept.f_dept_id
    and eox_dept_index.f_dept_pid = #v_dept_id
    and eox_user._update_flag > 0


-- add <> '' #v_login_id
	and eox_user.f_login_id like '%#v_login_id%'
-- end

-- add <> '' #v_name
	and eox_user.f_name like '%#v_name%'
-- end

-- add <> '' #v_phone
	and eox_user.f_phone like '%#v_phone%'
-- end

order by eox_user.f_dept_id,eox_user.f_user_id

-- end
