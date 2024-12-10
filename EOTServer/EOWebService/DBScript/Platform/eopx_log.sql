
-- inc #t_log_id
insert into eox_log(
	f_user_id,
	f_address,
	f_url,
	f_request,
	f_response,
	f_log_time,
	_update_time,
	_update_flag)
    values(
    #v_user_id,
	'#v_address',
	'#v_url',
	'#v_request',
	'#v_response',
	'##now',
	'##now', 1);
-- end

-- set
select 0 as _d, '' as _s, #t_log_id as f_log_id;
-- end