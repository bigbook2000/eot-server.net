
---- 编辑账号信息

---- iif 为条件复合语句
-- iff > 0 #v_user_id

	---- 不更新密码

	-- set
	update eox_user set
		f_login_id='#v_login_id',
		f_name='#v_name',
		f_dept_id=#v_dept_id,
		f_role='#v_role',
		f_sex='#v_sex',
		f_phone='#v_phone',
        f_location='#v_location',
		f_status=#v_status,
		f_note='#v_note',
		f_data_ex='#v_data_ex',
		_update_time=##now
        where f_user_id = #v_user_id;
	-- end

-- end

-- iff <= 0 #v_user_id

	---- 插入之后返回自增id
	-- inc #v_user_id
	insert into eox_user(
		f_login_id,
		f_login_psw,
		f_name,
		f_dept_id,
		f_role,
		f_sex,
		f_phone,
		f_location,
		f_status,
		f_note,
		f_data_ex,
		_update_time,
		_update_flag
		) values (
		#v_login_id,
		#v_login_psw,
		'#v_name',
		#v_dept_id,
		'#v_role',
		'#v_sex',
		'#v_phone',
		'#v_location',
		#v_status,
		'#v_note',
		'#v_data_ex',
		##now,
		1);
	-- end

-- end

-- set
select eox_user.*, '' as f_login_psw, 
	eox_dept.f_name as f_dept_id_s
	from eox_user, eox_dept
	where eox_user.f_dept_id = eox_dept.f_dept_id
    and eox_user.f_user_id = #v_user_id;
-- end
