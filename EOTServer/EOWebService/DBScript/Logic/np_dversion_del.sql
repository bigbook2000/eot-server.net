	
-- set
update n_dversion set _update_flag=-1 where f_version_id = #v_version_id;
-- end

-- set
select 0 as _d, '' as _s, #v_version_id as f_version_id;
-- end