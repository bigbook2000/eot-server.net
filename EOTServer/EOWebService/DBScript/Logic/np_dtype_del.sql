
---- 先删除版本
-- set
update n_dversion set _update_flag=-1 where f_type_id = #v_type_id;
-- end

-- set
update n_dtype set _update_flag=-1 where f_type_id = #v_type_id;
-- end
-- set
select 0 as _d, '' as _s, #v_type_id as f_type_id;
-- end