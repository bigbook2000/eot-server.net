
---- 列出指定类型和标识的所有文件

-- set
select eox_file.* from eox_file 
    where _update_flag>0 
    and f_type='#v_type' and f_keyid=#v_keyid and f_index=#v_index;
-- end