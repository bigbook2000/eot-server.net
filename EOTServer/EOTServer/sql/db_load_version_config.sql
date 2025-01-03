
-- var 
select f_keyid as t_keyid from eox_file where f_type='file_eotapp_bin' and f_sign='#v_sign';
-- end

-- set
select f_config_data from n_dversion where f_dversion_id=#t_keyid;
-- end