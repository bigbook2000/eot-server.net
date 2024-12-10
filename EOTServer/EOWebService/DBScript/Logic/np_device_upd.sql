	
---- 只能更新设备本身信息
---- iff复合条件语句 空格分割，依次为 比较运算符 比较值 参数1 ... 参数n
-- iff > 0 #v_device_id
    -- set
	update n_device set
        f_dept_id = #v_dept_id,
		f_mn = '#v_mn',
        f_name = '#v_name',
        f_enable = #v_enable,
		f_note = '#v_note',
		_update_time = ##now
        where f_device_id = #v_device_id;
    -- end
-- end
-- iff <= 0 #v_device_id
    -- inc #v_device_id
	insert into n_device(
		f_dkey,
        f_dtype,
        f_dversion,
        f_dept_id,
		f_mn,
        f_name,
        f_ctime,
        f_enable,
		f_note,
		_update_time, _update_flag)
		values ( 
		'',
        '',
        '',
        #v_dept_id,
        '#v_mn',
        '#v_name',
        ##now,
        #v_enable,
		'#v_note',
		##now, 1);
    -- end
-- end
        
-- set
select 0 as _d, '' as _s, n_device.*, n_data_rt.*,
    n_device.f_device_id as f_device_id,
    eox_dept.f_name as f_dept_id_s
    from n_device left join n_data_rt
    on (n_device.f_device_id = n_data_rt.f_device_id), eox_dept
    where n_device.f_device_id = #v_device_id
    and n_device.f_dept_id = eox_dept.f_dept_id;
-- end