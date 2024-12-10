   
---- 如果旧密码是空，不验证
-- iff <> '' #v_login_psw_old
	-- var
	select f_user_id as t_user_id from eox_user where f_user_id=#v_user_id and f_login_psw=#v_login_psw_old;
	-- end
-- end

-- iff = '' #v_login_psw_old
	-- var
	select #v_user_id as t_user_id;
	-- end
-- end

-- iff > 0 #t_user_id
	---- 更新密码

	-- set
	UPDATE eox_user set f_login_psw=#v_login_psw_new where f_user_id=#v_user_id;
	-- end

	-- set
	select 0 as _d, '' as _s, #v_user_id as f_user_id;
	-- end

-- end

-- iff <= 0 #t_user_id

	-- set
	select -1 as _d, '旧密码错误' as _s, #v_user_id as f_user_id;
	-- end

-- end
