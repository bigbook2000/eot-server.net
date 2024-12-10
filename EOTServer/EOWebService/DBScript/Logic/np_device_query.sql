
---- 执行指定的数据库，如果不指定，默认第一个
-- use mysql eotgate

---- 查询设备信息


-- set
select n_device.*, n_data_rt.*,
    n_device.f_device_id as f_device_id,
    eox_dept.f_name as f_dept_id_s
    from n_device left join n_data_rt
    on (n_device.f_device_id = n_data_rt.f_device_id), eox_dept
    where n_device._update_flag>0 
    and n_device.f_dept_id = eox_dept.f_dept_id 
    and n_device.f_ctime >= '#v_start_ctime' and n_device.f_ctime<'#v_end_ctime'

-- add <> '' #v_dkey
	and n_device.f_dkey like '%#v_dkey%'
-- end

-- add <> '' #v_dtype
    and n_device.f_dtype like '%#v_dtype%'
-- end

-- add <> '' #v_dversion
	and n_device.f_dversion like '%#v_dversion%'
-- end
    
-- add <> '' #v_mn
    and n_device.f_mn like '%#v_mn%'
-- end

-- add <> '' #v_name
    and n_device.f_name like '%#v_name%'
-- end
   

#v_order_by
-- end

