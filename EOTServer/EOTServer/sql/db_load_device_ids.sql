
---- #v_mns变量需带字符串

-- set
select f_device_id, f_mn FROM n_device WHERE f_mn in(#v_mns)
-- end