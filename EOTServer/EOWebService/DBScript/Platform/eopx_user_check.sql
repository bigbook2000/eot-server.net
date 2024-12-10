-- set
select eox_user.*,
	'' as f_login_psw,
    eox_dept.f_name as f_dept_id_s
	from eox_user, eox_dept 
    where eox_user.f_dept_id = eox_dept.f_dept_id 
    and eox_user.f_login_id = '#v_login_id' AND eox_user.f_login_psw = '#v_login_psw'
    and eox_user.f_status > 0
    and eox_user._update_flag > 0
    and eox_dept._update_flag > 0
-- end
